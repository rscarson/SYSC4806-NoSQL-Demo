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

        public Matrix(Matrix source) {
            Contents = new double[source.Columns(), source.Rows()];
            for (int col = 0; col < source.Columns(); col++) {
                for (int row = 0; row < source.Rows(); row++) {
                    Contents[col, row] = source.Contents[col, row];
                }
            }
        }

        /// <summary>
        /// Matrix multiplication
        /// </summary>
        /// <param name="l">Left matrix</param>
        /// <param name="r">Right matrix</param>
        /// <returns>Result</returns>
        public static Matrix operator /(Matrix l, double n) {
            Matrix result = new Matrix(l.Columns(), l.Rows());
            l.ForEach((col, row, value) => {
                result[col, row] = value / n;
            });

            return result;
        }

        /// <summary>
        /// Matrix multiplication
        /// </summary>
        /// <param name="l">Left matrix</param>
        /// <param name="r">Right matrix</param>
        /// <returns>Result</returns>
        public static Matrix operator *(Matrix l, Matrix r) {
            if (l.Columns() != r.Rows())
                throw new ArgumentException("Incompatibly sized matrices");

            Matrix result = new Matrix(r.Columns(), l.Rows());
            result.ForEach((col, row, value) => {
                result[col, row] = Enumerable.Range(0, l.Columns()).Sum(k => l[k, row] * r[col, k]);
            });

            return result;
        }

        /// <summary>
        /// Matrix addition
        /// </summary>
        /// <param name="l">Left matrix</param>
        /// <param name="r">Right matrix</param>
        /// <returns>Result</returns>
        public static Matrix operator +(Matrix l, Matrix r) {
            if (l.Columns() != r.Columns() || l.Rows() != r.Rows())
                throw new ArgumentException("Incompatibly sized matrices");

            Matrix result = new Matrix(l);
            for (int col = 0; col < result.Columns(); col++) {
                for (int row = 0; row < result.Rows(); row++) {
                    result[col, row] += r[col, row];
                }
            }

            return result;
        }

        /// <summary>
        /// Transpose matrix
        /// </summary>
        /// <returns>New matrix</returns>
        public Matrix Transpose() {
            Matrix transposed = new Matrix(Rows(), Columns());
            ForEach((col, row, v) => transposed[row, col] = v);
            return transposed;
        }

        /// <summary>
        /// Get a new matrix smaller than the old one
        /// </summary>
        /// <param name="columns">Width</param>
        /// <param name="rows">Height</param>
        /// <returns>New matrix</returns>
        public Matrix Truncate(int columns, int rows) {
            Matrix transformed = new Matrix(columns, rows);
            ForEach((col, row, v) => {
                if (col < columns && row < rows)
                    transformed[col, row] = v;
            });

            return transformed;
        }

        /// <summary>
        /// Eigenvector
        /// </summary>
        /// <returns>Eigen</returns>
        public Matrix EigenVector() {
            if (Columns() != Rows())
                throw new ArgumentException("Incompatibly sized matrices");
            Matrix b = new Matrix(1, Columns());
            b.ForEach((c, r, v) => b[c, r] = 1);

            foreach (int i in Enumerable.Range(0, 100)) {
                var m = this * b;
                b = m / m.Length();
            }

            Matrix result = new Matrix(Columns(), Rows());
            b.ForEach((col, row, value) => result[row, row] = value);
            return result;
        }

        /// <summary>
        /// Size of the vector
        /// </summary>
        /// <returns>Vector size</returns>
        public double Length() {
            double sum = 0;
            ForEach((col, row, value) => sum += value * value);
            return Math.Sqrt(sum);
        }

        /// <summary>
        /// Perform dimensional reduction
        /// </summary>
        /// <param name="dimensions">target dimensions</param>
        /// <returns>Reduced vector</returns>
        public Matrix Reduce(int dimensions) {
            Matrix sigma = this * this.Transpose();
            Matrix e = sigma.EigenVector();
            e = e.Truncate(dimensions, e.Rows()).Transpose();

            return e * this;
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
                db.DropCollection(collection);

                var col = db.GetCollection<BsonDocument>(collection);
                col.Insert(this.ToBSON(name));
            }
        }

        /// <summary>
        /// Load matrix from the database
        /// </summary>
        /// <param name="columns">Width</param>
        /// <param name="rows">Height</param>
        /// <param name="name">Collection name</param>
        /// <param name="name">Database name</param>
        /// <returns></returns>
        public static Matrix Load(int columns, int rows, string collection, string name) {
            using (var db = new LiteDatabase(DatabasePath)) {
                var col = db.GetCollection<BsonDocument>(collection);
                var result = col.FindOne(Query.EQ("name", name));

                if (result == null) {
                    Matrix matrix = Matrix.Random(columns, rows);
                    matrix.Save(collection, name);
                    return matrix;
                }

                return Matrix.FromBSON(result);
            }
        }

        /// <summary>
        /// Load all matching matrices
        /// </summary>
        /// <param name="name">Collection name</param>
        /// <param name="name">Database name</param>
        /// <returns></returns>
        public static Dictionary<string, Matrix> LoadAll(string collection) {
            Dictionary<string, Matrix> list = new Dictionary<string, Matrix>();
            using (var db = new LiteDatabase(DatabasePath)) {
                var col = db.GetCollection<BsonDocument>(collection);

                foreach (BsonDocument doc in col.FindAll())
                    list[doc["name"]] = Matrix.FromBSON(doc);
            }

            return list;
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
