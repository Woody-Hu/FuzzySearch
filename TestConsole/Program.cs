using FuzzySearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {

        }
    }

    class TestBean : ISearchItem
    {
        private string useName;

        internal TestBean(string inputName)
        {
            useName = inputName;
        }

        public string ItemName
        {
            get
            {
                return useName;
            }
        }
    }


}
