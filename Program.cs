using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace java.serialize
{
    class Program
    {
        class MainClass
        {
            static void Main(string[] args)
            {
                var path = @"/Users/yun/Desktop/list.ser";
                var sd = new SerializationDumper(path);
                //FileStream f = new FileStream(args[0], FileMode.Open, FileAccess.Read);
                //BinaryReader br = new BinaryReader(f);
                if (!sd.InitOk)
                    return;

                if (!sd.ParseStream())
                    Console.WriteLine("Invalid STREAM_MAGIC, should be 0xac ed");

                foreach (var cd in sd.ClassData)
                {
                    foreach (var classDetail in cd.ClassDetails)
                    {
                        if (classDetail.ClassName == "List")
                        {
                            foreach (ClassField field in classDetail.GetFields())
                            {
                                if (field.GetName() == "next")
                                {
                                    
                                }

                            }
                        }
                    }

                    Console.WriteLine(cd);
                }
            }
        }
    }
}
