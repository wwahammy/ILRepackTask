using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.ServiceLocation;


namespace BasicSample
{
    public class Class1
    {
        public Class1()
        {
            var locator = ServiceLocator.Current;
        }
    }
}
