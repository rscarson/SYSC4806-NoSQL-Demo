using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentsDemo {
    public class Matrix {
        public delegate void MatrixValueHandler(int col, int row, double v);
        public const string DatabasePath = @"datastore.db";

        public double[,] Contents { get; set; }

        public int Rows() {
            return Contents.GetLength(1);
        }

        public int Columns() {
            return Contents.GetLength(0);
        }

        public Matrix(int columns, int rows) {
            Contents = new double[columns, rows];
        }

        /// <summary>
        /// Array accessors
        /// </summary>
        /// <param name="col">Column</param>
        /// <param name="row">Row</param>
        /// <returns></returns>
        public double this[int col, int row] {
            get {
                return Contents[col, row];
            }

            set {
                Contents[col, row] = value;
            }
        }

        /// <summary>
        /// Randomly initialize the matrix
        /// </summary>
        /// <param name="columns">Columns in the matrix</param>
        /// <param name="rows">Rows in the matrix</param>
        public static Matrix Random(int columns, int rows) {
            Matrix matrix = new Matrix(columns, rows);
            Random random = new Random();
            matrix.ForEach((col, row, value) => {
                matrix[col, row] = random.NextDouble();
            });

            return matrix;
        }

        /// <summary>
        /// Save to database
        /// </summary>
        /// <param name="name">Collection name</param>
        /// <param name="name">Name of the database</param>
        public void Save(string collection, string name) {
            using (var db = new LiteDatabase(DatabasePath)) {
                var col = db.GetCollection<BsonDocument>(collection);
                
                col.Delete(Query.EQ("name", name));
                col.Insert(this.ToBSON(name));
            }
        }

        /// <summary>
        /// Load matrix from the database
        /// </summary>
        /// <param name="name">Collection name</param>
        /// <param name="name">Database name</param>
        /// <returns></returns>
        public static Matrix Load(string collection, string name) {
            using (var db = new LiteDatabase(DatabasePath)) {
                var col = db.GetCollection<BsonDocument>(collection);
                var result = col.FindOne(Query.EQ("name", name));

                if (result == null) {
                    return null;
                }

                return Matrix.FromBSON(result);
            }
        }

        /// <summary>
        /// List all matrices stored in the database
        /// </summary>
        /// <param name="name">Collection name</param>
        /// <returns></returns>
        public static string[] List(string collection) {
            List<string> documents = new List<string>();
            using (var db = new LiteDatabase(DatabasePath)) {
                var col = db.GetCollection<BsonDocument>(collection);
                foreach (var document in col.FindAll()) {
                    documents.Add(document["name"]);
                }

            }

            return documents.ToArray();
        }

        /// <summary>
        /// Iterator
        /// </summary>
        /// <param name="handler">col, row, value</param>
        public void ForEach(MatrixValueHandler handler) {
            for (int col = 0; col < Columns(); col++) {
                for (int row = 0; row < Rows(); row++) {
                    handler.Invoke(col, row, Contents[col, row]);
                }
            }
        }

        /// <summary>
        /// Convert from database format
        /// </summary>
        /// <param name="document">Input doc</param>
        /// <returns>Matrix if found, or new random matrix</returns>
        public static Matrix FromBSON(BsonDocument document) {
            var contents = document["contents"];

            int columns = contents.AsArray.Count, rows = contents.AsArray[0].AsArray.Count;
            Matrix matrix = new Matrix(columns, rows);

            for (int col = 0; col < columns; col++) {
                for (int row = 0; row < rows; row++) {
                    matrix[col, row] = contents.AsArray[col].AsArray[row];
                }
            }

            return matrix;
        }

        /// <summary>
        /// Convert a matrix to BSON format
        /// </summary>
        /// <param name="name">Name of the matrix</param>
        /// <returns>BSON formatted object</returns>
        public BsonDocument ToBSON(string name) {
            var document = new BsonDocument();
            document["name"] = name;

            var contents = new BsonArray();
            for (int col = 0; col < Columns(); col++) {
                var column = new BsonArray();
                for (int row = 0; row < Rows(); row++) {
                    column.Add(Contents[col, row]);
                }

                contents.Add(column);
            }

            document["contents"] = contents;
            return document;
        }

        /// <summary>
        /// Convert to readable form
        /// </summary>
        /// <returns>Matrix string</returns>
        public new String ToString() {
            string value = "";
            for (int row = 0; row < Rows(); row++) {
                for (int col = 0; col < Columns(); col++) {
                    value += string.Format("{0:F2} ", this[col, row]);
                }

                value += "\n";
            }

            return value;
        }
    }
}
