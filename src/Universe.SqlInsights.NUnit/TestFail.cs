using System;

public class TestFail : Exception
{
    public TestFail(string message) : base(message)
    {
    }
}