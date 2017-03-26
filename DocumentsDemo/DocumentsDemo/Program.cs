using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentsDemo {
    class Program {
        static void Main(string[] args) {
            if (args.Count() != 2) {
                Console.WriteLine("Usage:\nDocumentsDemo new <name>\nDocumentsDemo load <name>");
                return;
            }

            Matrix matrix;
            switch (args[0]) {
                case "new":
                    matrix = Matrix.Random(4, 4);
                    matrix.Save("matrices", args[1]);
                    Console.WriteLine(string.Format("Created a new matrix:\n{0}", matrix.ToString()));
                    break;

                case "load":
                    matrix = Matrix.Load(4, 4, "matrices", args[1]);
                    matrix.Save("matrices", args[1]);
                    Console.WriteLine(string.Format("Loaded an existing matrix:\n{0}", matrix.ToString()));
                    break;
            }
        }
    }
}
