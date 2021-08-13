using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSN.Matrix
{
    /// <summary>
    /// Класс реализует методы для работы с двумерными массивами
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Matrix<T>
    {
        private T[,] _data;

		public Matrix(T[,] data)
        {
            _data = data;
        }

        /// <summary>
        /// Вычленение из матрицы блоков, содержащих данные с одинаковым значением.
        /// </summary>
        /// <returns>Список классов, описывающих координаты блоков в матрице.</returns>
		public List<Block> GetMatrixBlocks()
        {
            List<Block> result = new List<Block>();
            int rows_count = _data.GetUpperBound(0);
            int columns_count = _data.GetUpperBound(1);
            int start_row = _data.GetLowerBound(0);
            int start_column = _data.GetLowerBound(1);

			for(int j = start_column; j <= columns_count; j++)
            {
				for(int i = start_row; i <= rows_count; i++)
                {
                    if (!result.Any(x=> x.IsContainCell(i, j)))
                    {
                        result.Add(FindBlock(i, j, rows_count, columns_count));                        
                    }                    
                }
            }
            return result;
        }
        /// <summary>
        /// Транспонирование матрицы
        /// </summary>
        /// <returns></returns>
        public T[,] Transponse()
        {
            T[,] result = new T[_data.GetLength(1), _data.GetLength(0)];
            
            for (int i = _data.GetLowerBound(0); i <= _data.GetUpperBound(0); i++)
            {
                for (int j = _data.GetLowerBound(1); j <= _data.GetUpperBound(1); j++)
                {
                    result[j, i] = _data[i, j];
                }
            }
            return result;
        }
        /// <summary>
        /// Текущая матрица
        /// </summary>
        /// <returns>Текущая матрица в виде двумерного массива</returns>
        public T[,] GetMatrix()
        {
            return _data;
        }

        /// <summary>
        /// Непосредственно вычисление блока с началом в заданных координатах.
        /// </summary>
        /// <param name="row">Индекс стартовой строки</param>
        /// <param name="column">Индекс стартового столбца</param>
        /// <param name="rows_ubound">Индекс граничной строки матрицы</param>
        /// <param name="columns_ubound">Индекс граничного столбца матрицы</param>
        /// <returns></returns>
        private Block FindBlock(int row, int column, int rows_ubound, int columns_ubound)
        {
            MatrixCell first_cell = new MatrixCell(row, column);
            MatrixCell last_cell = new MatrixCell(row, column);
            
            T prev_value = _data[row, column];
            bool is_all_rows;

            // Алгоритм прост: сначала поиск максимального количества строк, которые попадут в блок, потом проверка столбцов на соответствие данных
            // в диапазоне строк, вычисленном на первом шаге.
            for (int i = row; i <= rows_ubound; i++)
            {
                if ((_data[i,column] != null && !_data[i, column].Equals(prev_value)) || (_data[i,column] == null && prev_value != null))
                {
                    break;
                }
                last_cell.Row = i;
                prev_value = _data[i, column];
            }            

            for (int j = column; j <= columns_ubound; j++)
            {
                is_all_rows = false;

                for (int i = row; i <= last_cell.Row; i++)
                {
                    if ((_data[i, j] != null && !_data[i, j].Equals(prev_value)) || (_data[i, j] == null && prev_value != null))
                    {
                        break;
                    }
                    prev_value = _data[i, j];
                    is_all_rows = i == last_cell.Row;                    
                }         
                
                if (is_all_rows)
                {
                    last_cell.Column = j;
                }
                else
                {
                    break;
                }
            }

            return new Block(first_cell, last_cell) { BlockValue = _data[row, column] };
        }

        #region Вспомогательные классы
        /// <summary>
        /// Класс, описывающий начальные и конечные координаты блока в матрице.
        /// </summary>
        public class Block
        {
            /// <summary>
            /// Значение в блоке
            /// </summary>
			public T BlockValue { get; set; }
            /// <summary>
            /// Координаты первой ячейки блока
            /// </summary>
			public MatrixCell FirstCell { get; set; }
            /// <summary>
            /// Координаты последней ячейки блока
            /// </summary>
			public MatrixCell LastCell { get; set; }
			public Block(MatrixCell first_cell, MatrixCell last_cell)
            {
                FirstCell = new MatrixCell();
                LastCell = new MatrixCell();

                FirstCell.Row = first_cell.Row < last_cell.Row ? first_cell.Row : last_cell.Row;
                FirstCell.Column = first_cell.Column < last_cell.Column ? first_cell.Column : last_cell.Column;

                LastCell.Row = first_cell.Row > last_cell.Row ? first_cell.Row : last_cell.Row; ;
                LastCell.Column = first_cell.Column > last_cell.Column ? first_cell.Column : last_cell.Column;
            }
            /// <summary>
            /// Содержит ли блок ячейку с заданными координатами
            /// </summary>
            /// <param name="row_index">Строчный индекс проверямой ячейки в матрице</param>
            /// <param name="column_index">Индекс столбца проверяемой ячейки в матрице</param>
            /// <returns>Истина - проверямая ячейка попадает в блок. Ложь - не попадает.</returns>
            public bool IsContainCell(int row_index, int column_index)
            {
                return FirstCell.Row <= row_index && row_index <= LastCell.Row && FirstCell.Column <= column_index && column_index <= LastCell.Column;
            }
            public override string ToString()
            {
                return $"({FirstCell.Row};{FirstCell.Column}) - ({LastCell.Row};{LastCell.Column})";
            }
        }
        /// <summary>
        /// Координаты ячейки матрицы
        /// </summary>
		public class MatrixCell
        {
			public int Row { get; set; }
			public int Column { get; set; }
            public MatrixCell() { }
            public MatrixCell(int row, int column)
            {
                Row = row;
                Column = column;
            }
        }
        #endregion Вспомогательные классы
    }
}
