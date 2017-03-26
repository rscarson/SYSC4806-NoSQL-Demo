using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentsDemo {
    class Program {
        public const string CollectionName = "matrices";
        public const string NewUsage = "DocumentsDemo new <columns> <rows> <name>";
        public const string LoadUsage = "DocumentsDemo load <name>";
        public const string ListUsage = "DocumentsDemo list";

        static void Main(string[] args) {
            if (args.Count() == 0) {
                Console.WriteLine("Usage:\n{0}\n{1}\n{2}", NewUsage, LoadUsage, ListUsage);
                return;
            }

            Matrix matrix;
            switch (args[0]) {
                case "new":
                    if (args.Count() != 4)
                        Console.WriteLine("Usage:\n{0}", NewUsage);

                    // Get size of new matrix
                    int columns = 0, rows = 0;
                    if (!int.TryParse(args[1], out columns) || !int.TryParse(args[2], out rows)) {
                        Console.WriteLine("Usage:\n{0}", NewUsage);
                    } else if (columns <= 0 || rows <= 0) {
                        Console.WriteLine("Number of rows and columns must be greater than 0");
                    }

                    // Create and save it
                    matrix = Matrix.Random(columns, rows);
                    matrix.Save(CollectionName, args[3]);
                    Console.WriteLine(string.Format("Created a new matrix:\n{0}", matrix.ToString()));
                    break;

                case "load":
                    if (args.Count() != 2)
                        Console.WriteLine("Usage:\n{0}", LoadUsage);

                    // Load existing matrix
                    matrix = Matrix.Load(CollectionName, args[1]);
                    if (matrix == null)
                        Console.WriteLine("No matrix found by that name");
                    else
                        Console.WriteLine(string.Format("Loaded an existing matrix:\n{0}", matrix.ToString()));
                    break;

                case "list":
                    Console.WriteLine("All saved matrices\n----------");
                    foreach (string name in Matrix.List(CollectionName))
                        Console.WriteLine(name);
                    break;

                default:
                    Console.WriteLine("Unknown mode; specify new or load");
                    break;
            }
        }
    }
}
