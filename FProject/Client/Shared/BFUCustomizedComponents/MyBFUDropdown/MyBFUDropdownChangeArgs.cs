using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorFluentUI
{
    public class MyBFUDropdownChangeArgs
    {
        public IMyBFUDropdownOption Option { get; set; }
        //[Obsolete] public string? Key { get; set; } 
        public bool IsAdded { get; set; }

        //public MyBFUDropdownChangeArgs(string key, bool isAdded)
        //{
        //    Key = key;
        //    IsAdded = isAdded;
        //}

        public MyBFUDropdownChangeArgs(IMyBFUDropdownOption option, bool isAdded)
        {
            Option = option;
            IsAdded = isAdded;
        }
    }
}
