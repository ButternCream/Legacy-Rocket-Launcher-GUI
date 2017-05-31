using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rocket_League_Customizer
{
    class ModSetting
    {
        /*
         * Name is the name of setting
         * Value is the value of the setting
         */
        public string name { get; set; }
        public string value { get; set; }

        public ModSetting(string n, string v)
        {
            name = n;
            value = v;
        }

        //Name Setter
        public void Name(string new_name)
        {
            name = new_name;
        }

        //Name Getter
        public string Name()
        {
            return name;
        }

        //Value Setter
        public void Value(string new_value)
        {
            value = new_value;
        }

        //Value Getter
        public string Value()
        {
            return value;
        }
    }
}
