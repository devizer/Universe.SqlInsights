using NUnit.Framework.Interfaces;

public class TempTestAssemblyAction : Attribute, ITestAction
{
    public ActionTargets Targets => ActionTargets.Suite | ActionTargets.Test;
    public void BeforeTest(ITest test)
    {
    }

    public void AfterTest(ITest test)
    {
        TempDebug.WriteLine($"[TempTestAssemblyAction.AfterTest] Invoked TestType='{test.TestType}' Name='{test.Name}'");
        if (test.TestType == "Assembly")
        {
            var letsDebug = "ok";
        }
    }

}