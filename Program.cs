Bank mainBank = new();
mainBank.SetName("main bank");

mainBank.RegisterAccount(new BankAccount() { FullName = "Account #1", id = 91 });
mainBank.RegisterAccount(new BankAccount() { FullName = "Account #2", id = 92 });

mainBank.SetCommand(new ExecutePayment() { amount = 50, targetAccount = mainBank.GetAccountById(91), OperationId = 10 });
mainBank.ExecuteCommand();      // Пополняем баланс первого аккаунта на 50

mainBank.SetCommand(new ExecutePayment() { amount = 100, targetAccount = mainBank.GetAccountById(92), OperationId = 11 });
mainBank.ExecuteCommand();      // Пополняем баланс первого аккаунта на 100

Console.WriteLine(mainBank.GetBankInfo());

mainBank.SetCommand(new ExecutePayment() { amount = -70, targetAccount = mainBank.GetAccountById(91), OperationId = 12 });
mainBank.ExecuteCommand();      // Пробуем списать 70 при балансе 50, итог - операция отменена

Console.WriteLine(mainBank.GetBankInfo());

mainBank.RevertOperation(11);      // Отменяем пополнение на 100
Console.WriteLine(mainBank.GetBankInfo());

mainBank.SetCommand(new ExecutePayment() { amount = 200, targetAccount = mainBank.GetAccountById(92), OperationId = 13 });
mainBank.ExecuteCommand();      // Начисляем 200
mainBank.SetCommand(new AddPercent() { percent = 5, targetAccount = mainBank.GetAccountById(92), OperationId = 21 });
mainBank.ExecuteCommand();      // И еще 5%, начисление 10

Console.WriteLine(mainBank.GetBankInfo());

mainBank.SetCommand(new ExecutePayment() { amount = -150, targetAccount = mainBank.GetAccountById(92), OperationId = 14 });
mainBank.ExecuteCommand();      // Списываем 150
mainBank.RevertOperation(21);   // Списываем проценты

Console.WriteLine(mainBank.GetBankInfo());

interface IAccountCommand
{
    void Execute();
    void Undo();
    void SetStatus(BankTransactionStatus status);

    BankTransactionStatus GetStatus();
    string GetOperationInfo();
    int GetOperationId();
}

enum BankTransactionStatus
{
    Accepted,
    Waiting,
    Processing,
    Cancelled,
    Reverted
}

class BankAccount
{
    public string? FullName;
    public int balance = 0;
    public int id;

    public string GetAccountInfo()
    {
        return $"Account name: {FullName}, balance: {balance}, id: {id}";
    }
}

class ExecutePayment : IAccountCommand
{
    public int amount;
    public BankTransactionStatus status = BankTransactionStatus.Waiting;
    public BankAccount? targetAccount;
    public int OperationId;

    public void SetTransactionAmount(int amount)
    {
        this.amount = amount;
    }

    public void SetTargetAccount(BankAccount account)
    {
        targetAccount = account;
    }

    public void Execute()
    {
        if (targetAccount != null)
        {
            int resultAmount = targetAccount.balance + amount;
            if (resultAmount < 0)
            {
                throw new InsufficientFundsException();
            }
            else
            {
                targetAccount.balance = resultAmount;
            }
        }
        else
        {
            throw new Exception("Account was not set. ");
        }
    }

    public void Undo()
    {
        if (targetAccount != null)
        {
            targetAccount.balance -= amount;
        }
        else
        {
            throw new Exception("Account was not set. ");
        }
    }

    public void SetStatus(BankTransactionStatus status)
    {
        this.status = status;
    }

    BankTransactionStatus IAccountCommand.GetStatus()
    {
        return status;
    }

    string GetOperationType()
    {
        if (amount > 0) return "Income";
        else return "Outcome";
    }

    public string GetOperationInfo()
    {
        return $"\tOperationType: Payment\n\tPayment type: {GetOperationType()}\n\tOperation Amount: {Math.Abs(amount)}\n\tStatus: {status}\n\tid: {OperationId}\n";
    }

    int IAccountCommand.GetOperationId()
    {
        return OperationId;
    }
}

class AddPercent : IAccountCommand
{
    public int percent;
    public int? percentAmount;
    public BankAccount? targetAccount;
    BankTransactionStatus status;
    public int OperationId;

    public void SetAccrualProcent(int procent)
    {
        percent = procent;
    }

    public void SetTargetAccount(BankAccount account)
    {
        targetAccount = account;
    }

    public void Execute()
    {
        if (targetAccount != null)
        {
            int new_balance = (int)(targetAccount.balance * (percent + 100f) / 100f);
            percentAmount = new_balance - targetAccount.balance;
            targetAccount.balance = new_balance;
        }
        else
        {
            throw new Exception("Account was not set. ");
        }
    }

    public void Undo()
    {
        if (targetAccount is not null)
        {
            if (percentAmount is null)
                throw new Exception("Percent was not accrued. ");
            else targetAccount.balance -= percentAmount.Value;
        }
        else
        {
            throw new Exception("Account was not set. ");
        }
    }

    public void SetStatus(BankTransactionStatus status)
    {
        this.status = status;
    }

    BankTransactionStatus IAccountCommand.GetStatus()
    {
        return status;
    }

    public string GetOperationInfo()
    {
        return $"\tOperationType: Percent accrual\n\tAccrual procent: {percent}%\n\tStatus: {status}\n\tid: {OperationId}\n";
    }

    int IAccountCommand.GetOperationId()
    {
        return OperationId;
    }
}

class Bank
{
    string? BankName;
    IAccountCommand? Cmd;

    readonly List<IAccountCommand> commands = [];
    readonly List<BankAccount> accounts = [];

    public void SetCommand(IAccountCommand cmd)
    {
        Cmd = cmd;
    }

    public void ExecuteCommand()
    {
        if (Cmd is null)
        {
            throw new Exception("Command was not set. ");
        }
        else if (CheckOperationExists(Cmd.GetOperationId()))
        {
            throw new Exception("Operation already executed. ");
        }
        else
        {
            int CurrentOperationIndex = commands.Count;
            commands.Add(Cmd);
            commands.ElementAt(CurrentOperationIndex).SetStatus(BankTransactionStatus.Processing);
            try
            {
                commands.ElementAt(CurrentOperationIndex).Execute();
                commands.ElementAt(CurrentOperationIndex).SetStatus(BankTransactionStatus.Accepted);
            }
            catch
            {
                commands.ElementAt(CurrentOperationIndex).SetStatus(BankTransactionStatus.Cancelled);
            }

        }
    }

    public void RevertOperation(int operationId)
    {
        foreach (var operation in commands)
        {
            if (operation.GetOperationId() == operationId)
            {
                operation.Undo();
                operation.SetStatus(BankTransactionStatus.Reverted);
                return;
            }
        }
        throw new Exception("Operation does not exists. ");
    }

    public void SetName(string name)
    {
        BankName = name;
    }

    public void RegisterAccount(BankAccount account)
    {
        foreach (var account_ in accounts)
        {
            if (account_.id == account.id) throw new Exception("Id already exists.");
        }
        accounts.Add(account);
    }

    public BankAccount GetAccountById(int id)
    {
        foreach (var account in accounts)
        {
            if (account.id == id) return account;
        }
        throw new Exception($"Account with id={id} was not found");
    }

    public bool CheckOperationExists(int id)
    {
        foreach (var operation in commands)
        {
            if (operation.GetOperationId() == id) return true;
        }
        return false;
    }

    public string GetBankInfo()
    {
        string result = "";

        result += $"Bank name: {BankName}, Total transactions: {commands.Count}, Total accounts: {accounts.Count}\n\n";
        foreach (var Operation in commands)
        {
            result += Operation.GetOperationInfo() + "\n";
        }

        foreach (var account in accounts)
        {
            result += account.GetAccountInfo() + "\n";
        }

        return result;
    }
}

public class InsufficientFundsException : Exception
{
    public InsufficientFundsException() { }

    public InsufficientFundsException(string message)
        : base(message) { }

    public InsufficientFundsException(string message, Exception inner)
        : base(message, inner) { }
}