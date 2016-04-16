using System;

namespace HighriseApi.Tests
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("Testing...");

            var company = new CompanyRequestTest();
            company.GetSingleTest();
            company.GetTest();

            var person = new PersonRequestTest();
            person.GetTest();

            Console.ReadKey();
        } 
    }
}
