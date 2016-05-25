using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Reflection;

namespace SchemeOfAlgorithm
{
    public static class SchemeDraw
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="acBlkTblRec">Needed for polyline3d before adding vertices</param>
        /// <param name="acTrans">Needed for polyline3d before adding vertices</param>
        /// <returns></returns>
        public static List<Entity> getSchemeEntities(BlockTableRecord acBlkTblRec, Transaction acTrans)
        {
            List<Entity> entitiesToDraw = new List<Entity>();
            int m, n;
            TableCell[,] table = readDataFromFile(out m, out n);
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                {
                    //If there is Shape in current cell of table
                    if (table[i, j].shape != null)
                    {
                        Shape currShape = table[i, j].shape;
                        //Add lines of current shape to entitiesToDraw
                        entitiesToDraw.AddRange(currShape.entitiesToDraw);

                        entitiesToDraw.AddRange(DrawShapeLines(table, m, n, i, j, currShape, acBlkTblRec, acTrans));
                    }
                }
            return entitiesToDraw;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="table"></param>
        /// <param name="m"></param>
        /// <param name="n"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="currShape"></param>
        /// <param name="acBlkTblRec">Needed for polyline3d before adding vertices</param>
        /// <param name="acTrans">Needed for polyline3d before adding vertices</param>
        /// <returns></returns>
        private static List<Entity> DrawShapeLines(TableCell[,] table, int m, int n, int i, int j, Shape currShape, BlockTableRecord acBlkTblRec, Transaction acTrans)
        {
            List<Entity> entitiesToDraw = new List<Entity>();
            foreach (List<string> currLine in table[i, j].shapeLines)
            {
                if (currLine.Count == 0)
                    continue;
                bool firstSegmentOfLine = true;
                Point3d cursorPt = new Point3d();
                Polyline3d polyline = new Polyline3d();
                acBlkTblRec.AppendEntity(polyline);
                acTrans.AddNewlyCreatedDBObject(polyline, true);
                polyline.SetDatabaseDefaults();
                int neighbourRow = i;
                int neighbourColumn = j;
                Shape neighbourShape;
                foreach (String dir in currLine)
                {
                    //Смотреть нужно ли различать CursorPt и ConnectionVertex
                    string direction = dir.ToLower();
                    if (direction != "down" && direction != "right" && direction != "left" && direction != "up")
                        throw new System.Exception(@"Invalid input of lines. Check input data file. Line must correspond to ""right"",""down"",""left"" or ""up""");
                    //May check direction
                    //If line just begins then add start connection point to polyline
                    if (firstSegmentOfLine)
                    {
                        switch (direction)
                        {
                            case "down": cursorPt = currShape.lowerConnectionPt; break;
                            case "right": cursorPt = currShape.rightConnectionPt; break;
                            case "left": cursorPt = currShape.leftConnectionPt; break;
                            case "up": cursorPt = currShape.upperConnectionPt; break;
                        }
                        //The same variable as for further vertices may be used (instead of distinct beginVertex)
                        PolylineVertex3d beginVertex = new PolylineVertex3d(cursorPt);
                        polyline.AppendVertex(beginVertex);
                        acTrans.AddNewlyCreatedDBObject(beginVertex, true);
                        firstSegmentOfLine = false;
                    }
                    //then add other vertices of polyline
                    switch (direction)
                    {
                        case "down": neighbourRow++; break;
                        case "right": neighbourColumn++; break;
                        case "left": neighbourColumn--; break;
                        case "up": neighbourRow--; break;
                    }
                    //check for overrun of draw field
                    if (!(neighbourRow < m && neighbourRow < n))
                        return null;
                    neighbourShape = table[neighbourRow, neighbourColumn].shape;
                    PolylineVertex3d connectionVertex = new PolylineVertex3d();
                    //check if there is shape in the next cell of field (table)
                    if (neighbourShape != null)
                    {
                        Point3d connectionPt = new Point3d();
                        
                        switch (direction)
                        {
                            case "down": connectionPt = neighbourShape.upperConnectionPt; break;
                            case "right": connectionPt = neighbourShape.leftConnectionPt; break;
                            case "left": connectionPt = neighbourShape.rightConnectionPt; break;
                            case "up": connectionPt = neighbourShape.lowerConnectionPt; break;
                        }

                        connectionVertex = new PolylineVertex3d(connectionPt);
                    }
                    else
                    {
                        connectionVertex = new PolylineVertex3d(table[neighbourRow, neighbourColumn].pt);
                    }
                    polyline.AppendVertex(connectionVertex);
                    acTrans.AddNewlyCreatedDBObject(connectionVertex, true);
                }
                entitiesToDraw.Add(polyline);
            }
            return entitiesToDraw;
        }
        enum ShapeTypes { Rectangle, SubProcess, Terminator, Condition, Cycle}
        public struct TableCell
        {
            public Shape shape;
            public Point3d pt;
            public List<List<string>> shapeLines;
        }
        private static TableCell[,] readDataFromFile(out int m, out int n)
        {
            m = n = 0;
            //Change it or put outside of method
            Point3d startPt = new Point3d(50, 255, 0);
            double xOffset = 24;
            double yOffset = 20;

            try
            {   // Open the text file using a stream reader.
                using (StreamReader file = new StreamReader("input.txt", Encoding.Default))
                {
                    //Read first line of file
                    //Read dimensions of matrix (net)
                    string[] dimensions = file.ReadLine().Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries); ;
                    m = int.Parse(dimensions[0]);//May be check of value and success of Parsing
                    n = int.Parse(dimensions[1]);

                    TableCell[,] table = new TableCell[m, n];
                    List<Shape> allShapes = new List<Shape>();
                    for (int i = 0; i < m; i++)
                        for (int j = 0; j < n; j++)
                            table[i, j].pt = startPt.Add(new Vector3d(j * xOffset, -i * yOffset, 0));


                    string source;
                    string[] inputString;

                    while ((source = file.ReadLine()) != null)
                    {
                        //Read all lines of file since second line
                        inputString = source.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        int i, j;
                        i = int.Parse(inputString[2]);
                        j = int.Parse(inputString[3]);
                        //Fill coordinates of all cells of table
                        
                        //Point to insert shape. Calculate relative to startpoint
                        //Point3d insertPt = startPt.Add(new Vector3d(j * xOffset, -i * yOffset, 0));
                        List<List<string>> shapeLines = new List<List<string>>();

                        var allShapeSubClasses = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                                                  from assemblyType in domainAssembly.GetTypes()
                                                  where assemblyType.IsSubclassOf(typeof(Shape))
                                                  select assemblyType).ToArray();

                        foreach (var shape in allShapeSubClasses)
                        {
                            //if shape in file is known. (Full shape.ToString() is SchemeOfAlgorithm.Rectangle, etc. So I use Contains method to determine only last part)
                            if (shape.Name.ToLower() == inputString[0].ToLower().Replace(" ", ""))
                            {
                                ConstructorInfo[] currTypeConstructors = shape.GetConstructors();
                                ParameterInfo[] constructorParams = currTypeConstructors[0].GetParameters();
                                object[] constructorParametersValues = new object[constructorParams.Length];
                                //first param is insert point for all shape derived classes
                                constructorParametersValues[0] = table[i,j].pt;
                                for (int t=0;t< constructorParams.Length;t++)
                                    if (constructorParams[t].HasDefaultValue)
                                        constructorParametersValues[t] = constructorParams[t].DefaultValue;
                                //add text to shape from input file
                                constructorParametersValues[1] = inputString[1];
                                //typeof(Rectangle).getco
                                table[i, j].shape = (Shape)Activator.CreateInstance(shape, constructorParametersValues);//inputString[1]);
                                //delete this line!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                                //table[i, j].pt = insertPt;
                                int k = 4;//index of input start position of polylines vertices
                                //list of current shape. May be more than 1
                                List<string> connectionLine = new List<string>();
                                bool firstRun = true;
                                for (int l = k; l < inputString.Length; l++)
                                {
                                    if (inputString[l].ToLower().Contains("line".ToLower()))
                                    {
                                        //При первом проходе не инициализируем лишний List
                                        if (!firstRun)
                                        {
                                            //Executes if there are more than 1 line
                                            connectionLine = new List<string>();
                                        }
                                        //List of all shape's outgoing lines
                                        shapeLines.Add(connectionLine);
                                        firstRun = false;
                                    }
                                    else
                                    {
                                        connectionLine.Add(inputString[l].ToString());
                                    }
                                }
                                table[i, j].shapeLines = shapeLines;
                            }
                        }
                    }
                    return table;
                }
            }
            finally { }
        }        
    }
}
