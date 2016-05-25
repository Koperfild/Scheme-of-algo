using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace SchemeOfAlgorithm
{
    public class DrawEntities
    {
        /// <summary>
        /// Create Line, move start point to its end
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="angle"></param>
        /// <param name="dist"></param>
        /// <returns></returns>
        public static Line createLine(ref Point3d startPoint, double angle, double dist)
        {
            Point3d endPt = new Point3d(startPoint.X + dist * Math.Cos(angle), startPoint.Y + dist * Math.Sin(angle),0);
            Line line = new Line(startPoint, endPt);
            startPoint = endPt;
            return line;
        }
        
    }
    public abstract class Shape
    {
        public Point3d upperConnectionPt { get; set; }
        public Point3d lowerConnectionPt { get; set; }
        public Point3d rightConnectionPt { get; set; }
        public Point3d leftConnectionPt { get; set; }
        public Point3d center { get; set; }
        protected double width { get; set; }
        protected double height { get; set; }
        /// <summary>
        /// ratio of width to height (width/height)
        /// </summary>
        protected double ratio { get; set; }
        /// <summary>
        /// Just create shape in some point and use its entitiesToDraw property to get all lines to draw
        /// </summary>
        /// <param name="center"></param>
        /// <param name="text"></param>
        /// <param name="width"></param>
        /// <param name="ratio"></param>
        public Shape(Point3d center, string text ="Default text", double width = 16, double ratio = 16.0/9.0)
        {
            this.ratio = ratio;
            this.width = width;
            this.height = this.width / this.ratio;
            this.center = center;
            this.upperConnectionPt = center.Add(new Vector3d(0, height / 2, 0));
            this.lowerConnectionPt = this.upperConnectionPt.Add(new Vector3d(0, -this.height, 0));
            this.rightConnectionPt = this.center.Add(new Vector3d(width / 2, 0, 0));
            this.leftConnectionPt = this.center.Add(new Vector3d(-width / 2, 0, 0));
            AddText(text);
        }
        protected abstract void Draw();
        public List<Entity> entitiesToDraw = new List<Entity>();
        public List<Shape> associatedShapes = new List<Shape>();
        //protected Entity textEntity;
        protected void AddText(string text)
        {
            MText textToShow = new MText();
            textToShow.Contents = text;
            textToShow.Location = this.center.Add(new Vector3d(-5,1,0));
            //textEntity = textToShow;
            entitiesToDraw.Add(textToShow);
        }
    }
    public class Rectangle:Shape
    {      
        protected Point3d leftUpPt;
        protected Point3d rightUpPt;
        protected Point3d rightDownPt;
        protected Point3d leftDownPt;

        //Can be made without all 4 points. Just draw beginning from upperConnectionPt. But center is required then to insert text
        public Rectangle(Point3d center, string text="Default", double width = 16, double ratio = 16.0/9.0):base(center, text, width, ratio)
        {
            this.leftUpPt = new Point3d(center.X - (width / 2), center.Y + (height / 2), 0);
            this.rightUpPt = new Point3d(center.X + (width / 2), center.Y + (height / 2), 0);
            this.rightDownPt = new Point3d(center.X + (width / 2), center.Y + (height / 2), 0);
            this.leftDownPt = new Point3d(center.X - (width / 2), center.Y - (height / 2), 0);
            Draw();
        }
        /// <summary>
        /// Creates entities composing rectangle
        /// </summary>
        /// <returns>List of created entities</returns>
        protected override void Draw()
        {
            //indicates current start drawing point
            Point3d cursorPoint = new Point3d();
            cursorPoint = this.leftUpPt;//clone new point(as it is structure)
            //Draw rectangle via 4 lines
            this.entitiesToDraw.Add(DrawEntities.createLine(ref cursorPoint, 0, width));
            this.entitiesToDraw.Add(DrawEntities.createLine(ref cursorPoint, 3 * Math.PI / 2, height));
            this.entitiesToDraw.Add(DrawEntities.createLine(ref cursorPoint, Math.PI, width));
            this.entitiesToDraw.Add(DrawEntities.createLine(ref cursorPoint, Math.PI / 2, height));
        }
    }
    public class SubProcess:Rectangle
    {
        private double innerOffset=2;
        public SubProcess(Point3d center, string text="Default", double offset = 2, double width = 16, double ratio = 16.0/9.0):base(center, text, width, ratio)
        {
            this.innerOffset = offset;
            Draw();
        }
        protected override void Draw()
        {
            base.Draw();
            Point3d cursorPoint = new Point3d();
            cursorPoint = leftUpPt;//clone new point(as it is structure)
            cursorPoint = cursorPoint.Add(new Vector3d(innerOffset,0,0));
            //draw left inner vertical line
            this.entitiesToDraw.Add(DrawEntities.createLine(ref cursorPoint, 3 * Math.PI / 2, height));
            cursorPoint = rightUpPt;
            cursorPoint = cursorPoint.Add(new Vector3d(-innerOffset, 0, 0));
            //draw fight inner vertical line
            this.entitiesToDraw.Add(DrawEntities.createLine(ref cursorPoint, 3 * Math.PI / 2, height));
        }
    }
    public class Terminator:Rectangle
    {
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="upperConnectionPt"></param>
        /// <param name="width">length of horizontal line</param>
        /// <param name="height"></param>
        public Terminator(Point3d center, string text="Default", double width = 16, double ratio = 16.0/5.0):base(center, text, width, ratio)
        {
            Draw();
            //height/2 is radius of side arcs
            this.rightConnectionPt = this.center.Add(new Vector3d(width / 2 + height / 2, 0, 0));
            this.leftConnectionPt = this.center.Add(new Vector3d(-(width / 2 + height / 2), 0, 0));
        }
        protected override void Draw()
        {
            
            //indicates current start drawing point
            Point3d cursorPoint = new Point3d();
            cursorPoint = this.leftUpPt;//clone new point(as it is structure)
            List<Entity> entitiesToDraw = new List<Entity>();
            this.entitiesToDraw.Add(DrawEntities.createLine(ref cursorPoint, 0, width));
            //Draw terminator
            entitiesToDraw.Add(DrawEntities.createLine(ref cursorPoint, 0, width));

            Arc arc = new Arc(this.center.Add(new Vector3d(this.width / 2, 0, 0)),//Move right from center
                
                this.height / 2, 
                              Math.PI/2,
                              3*Math.PI/2);
            //Matrix to mirror arc . (0,0,1) - vector perpendicularly WCS plane
            Matrix3d matr = Matrix3d.Mirroring(this.center.Add(new Vector3d(this.width / 2, 0, 0)));//(new Line3d(center, center.Add(new Vector3d(0, 1, 0))));//arc.Center, new Vector3d(1, 0, 0)));
            
            cursorPoint = cursorPoint.Add(new Vector3d(0, -height, 0));// arc.EndPoint;//used further for createLine
            arc.TransformBy(matr);
            this.entitiesToDraw.Add(arc);

            this.entitiesToDraw.Add(DrawEntities.createLine(ref cursorPoint, Math.PI, width));

            //2nd arc is drown clockwise to avoid mirroring
            Arc arc2 = new Arc(this.center.Add(new Vector3d(-this.width / 2.0, 0, 0)),//Move left from center
                               this.height / 2,
                               Math.PI/2,
                               3 * Math.PI/2);
            this.entitiesToDraw.Add(arc2);
        }
    }
    public class Condition:Shape
    {
        public Condition(Point3d center, string text = "Default", double width = 16,double ratio = 16.0/9.0):base(center, text, width, ratio)
        {
            Draw();
        }
        protected override void Draw()
        {
            Point3d cursorPt = new Point3d();
            cursorPt = upperConnectionPt;
            this.entitiesToDraw.Add(new Line(upperConnectionPt, rightConnectionPt));
            this.entitiesToDraw.Add(new Line(rightConnectionPt, lowerConnectionPt));
            this.entitiesToDraw.Add(new Line(lowerConnectionPt, leftConnectionPt));
            this.entitiesToDraw.Add(new Line(leftConnectionPt, upperConnectionPt));
        }
    }
    public class Cycle:Shape
    {
        //width of cycle is the shortest line length without offset
        double offset;
        public Cycle(Point3d center, string text ="Default", double width = 16, double ratio = 16.0/7.5, double offset = 2):base(center, text, width, ratio)
        {
            this.offset = offset;
            this.rightConnectionPt = this.center.Add(new Vector3d(width / 2 + offset, 0, 0));
            this.leftConnectionPt = this.center.Add(new Vector3d(-(width / 2 + offset), 0, 0));

            Draw();
        }
        protected override void Draw()
        {
            Point3d cursorPt = new Point3d();
            cursorPt = this.upperConnectionPt;

            List<Point3d> PerimeterPoints = new List<Point3d>();
            PerimeterPoints.Add(cursorPt);
            cursorPt = cursorPt.Add(new Vector3d(width / 2.0, 0, 0));
            PerimeterPoints.Add(cursorPt);
            cursorPt = cursorPt.Add(new Vector3d(offset, -offset, 0));//edge of shape
            PerimeterPoints.Add(cursorPt);
            cursorPt = cursorPt.Add(new Vector3d(0, -(height - offset), 0));
            PerimeterPoints.Add(cursorPt);
            cursorPt = cursorPt.Add(new Vector3d(-(width+2*offset), 0, 0));
            PerimeterPoints.Add(cursorPt);
            cursorPt = cursorPt.Add(new Vector3d(0, height - offset, 0));
            PerimeterPoints.Add(cursorPt);
            cursorPt = cursorPt.Add(new Vector3d(offset, offset, 0));
            PerimeterPoints.Add(cursorPt);
            PerimeterPoints.Add(this.upperConnectionPt);//enclose point (start point)

            for (int i=0;i<PerimeterPoints.Count-1;i++)
            {
                this.entitiesToDraw.Add(new Line(PerimeterPoints[i], PerimeterPoints[i + 1]));
            }
        }
    }
    public class CycleEnd : Shape
    {
        double offset;
        public CycleEnd(Point3d center, string text = "Default", double width = 16, double ratio = 16.0 / 7.5, double offset = 2) : base(center, text, width, ratio)
        {
            this.offset = offset;
            this.rightConnectionPt = this.center.Add(new Vector3d(width / 2 + offset, 0, 0));
            this.leftConnectionPt = this.center.Add(new Vector3d(-(width / 2 + offset), 0, 0));

            Draw();
        }
        protected override void Draw()
        {
            Point3d cursorPt = new Point3d();
            cursorPt = this.upperConnectionPt;

            List<Point3d> PerimeterPoints = new List<Point3d>();
            PerimeterPoints.Add(cursorPt);
            cursorPt = cursorPt.Add(new Vector3d(width/2 + offset, 0, 0));
            PerimeterPoints.Add(cursorPt);
            cursorPt = cursorPt.Add(new Vector3d(0, -(height - offset), 0));
            PerimeterPoints.Add(cursorPt);
            cursorPt = cursorPt.Add(new Vector3d(-offset, -offset, 0));
            PerimeterPoints.Add(cursorPt);
            cursorPt = cursorPt.Add(new Vector3d(-width, 0, 0));
            PerimeterPoints.Add(cursorPt);
            cursorPt = cursorPt.Add(new Vector3d(-offset, offset, 0));
            PerimeterPoints.Add(cursorPt);
            cursorPt = cursorPt.Add(new Vector3d( 0, height - offset, 0));
            PerimeterPoints.Add(cursorPt);
            PerimeterPoints.Add(this.upperConnectionPt);//enclose point (start point)

            for (int i = 0; i < PerimeterPoints.Count - 1; i++)
            {
                this.entitiesToDraw.Add(new Line(PerimeterPoints[i], PerimeterPoints[i + 1]));
            }
        }
    }

    //Test drawing of shapes
    public class TestSomeShit
    {
        [CommandMethod("test1")]
        public void test1()
        {
            Cycle cycle = new Cycle(new Point3d(100, 100, 0));
            List<Entity> entitiesToDraw = cycle.entitiesToDraw;

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                                             OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;

                // Add the new object to the block table record and the transaction
                foreach (Entity currEntity in entitiesToDraw)
                {
                    acBlkTblRec.AppendEntity(currEntity);
                    acTrans.AddNewlyCreatedDBObject(currEntity, true);
                }

                // Save the new line to the database
                acTrans.Commit();
            }
        }
        [CommandMethod("test2")]
        public void test2()
        {
            /*var allShapeSubClasses = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                            from assemblyType in domainAssembly.GetTypes()
                            where assemblyType.IsSubclassOf(typeof(Shape))
                            select assemblyType).ToArray();
            
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assemblyType11 = assemblies[0].GetTypes();
            assemblyType.Contains(typeof(Rectangle));
            
            List<Shape> allfigures = new List<Shape>();
            Point3d startPt = new Point3d(100,100,0);
            double downOffset = 20;
            foreach (var currClass in allShapeSubClasses)
            {
                allfigures.Add((Shape)Activator.CreateInstance(currClass, startPt));
                
                startPt.Add(new Vector3d(0, -downOffset, 0));
            }
            */

            /*
            List<Shape> allfigures = new List<Shape>();
            List<Type> types = new List<Type>();
            types.Add(typeof(Rectangle));
            types.Add(typeof(SubProcess));
            types.Add(typeof(Terminator));
            types.Add(typeof(Condition));
            types.Add(typeof(Cycle));

            Point3d startPt = new Point3d(100, 100, 0);
            double downOffset = 20;
            foreach (var type in types)
            {
                allfigures.Add((Shape)Activator.CreateInstance(type, new object[] { startPt }));
                startPt = ((Point3d)startPt).Add(new Vector3d(0, -downOffset, 0));
            }
            */
            Point3d startPt = new Point3d(100, 100, 0);
            double downOffset = 20;
            Rectangle rect = new Rectangle(startPt);
            startPt = ((Point3d)startPt).Add(new Vector3d(0, -downOffset, 0));
            SubProcess subproc = new SubProcess(startPt);
            startPt = ((Point3d)startPt).Add(new Vector3d(0, -downOffset, 0));
            Terminator term = new Terminator(startPt);
            startPt = ((Point3d)startPt).Add(new Vector3d(0, -downOffset, 0));
            Condition cond = new Condition(startPt);
            startPt = ((Point3d)startPt).Add(new Vector3d(0, -downOffset, 0));
            Cycle cycle = new Cycle(startPt);
            startPt = ((Point3d)startPt).Add(new Vector3d(0, -downOffset, 0));
            CycleEnd cycleEnd = new CycleEnd(startPt);
            
            List<Entity> entitiesToDraw = new List<Entity>();
            entitiesToDraw.AddRange(rect.entitiesToDraw);
            entitiesToDraw.AddRange(subproc.entitiesToDraw);
            entitiesToDraw.AddRange(term.entitiesToDraw);
            entitiesToDraw.AddRange(cond.entitiesToDraw);
            entitiesToDraw.AddRange(cycle.entitiesToDraw);
            entitiesToDraw.AddRange(cycleEnd.entitiesToDraw);
                       
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                                             OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;

                // Add the new object to the block table record and the transaction
                foreach (Entity currEntity in entitiesToDraw)
                {
                    acBlkTblRec.AppendEntity(currEntity);
                    acTrans.AddNewlyCreatedDBObject(currEntity, true);
                }

                // Save the new line to the database
                acTrans.Commit();
            }
        }

        [CommandMethod("rectangle")]
        public void rectangle()
        {
            Rectangle rect = new Rectangle(new Point3d(100, 100, 0));
            List<Entity> entitiesToDraw = new List<Entity>();
            entitiesToDraw.AddRange(rect.entitiesToDraw);

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                                             OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;

                // Add the new object to the block table record and the transaction
                foreach (Entity currEntity in entitiesToDraw)
                {
                    acBlkTblRec.AppendEntity(currEntity);
                    acTrans.AddNewlyCreatedDBObject(currEntity, true);
                }

                // Save the new line to the database
                acTrans.Commit();
            }
        }
        [CommandMethod("SubProcess")]
        public void SubProcess()
        {
            SubProcess rect = new SubProcess(new Point3d(100, 100, 0));
            List<Entity> entitiesToDraw = new List<Entity>();
            entitiesToDraw.AddRange(rect.entitiesToDraw);

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                                             OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;

                // Add the new object to the block table record and the transaction
                foreach (Entity currEntity in entitiesToDraw)
                {
                    acBlkTblRec.AppendEntity(currEntity);
                    acTrans.AddNewlyCreatedDBObject(currEntity, true);
                }

                // Save the new line to the database
                acTrans.Commit();
            }
        }
        [CommandMethod("Terminator")]
        public void Terminator()
        {
            Terminator rect = new Terminator(new Point3d(100, 100, 0));
            List<Entity> entitiesToDraw = new List<Entity>();
            entitiesToDraw.AddRange(rect.entitiesToDraw);

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                                             OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;

                // Add the new object to the block table record and the transaction
                foreach (Entity currEntity in entitiesToDraw)
                {
                    acBlkTblRec.AppendEntity(currEntity);
                    acTrans.AddNewlyCreatedDBObject(currEntity, true);
                }

                // Save the new line to the database
                acTrans.Commit();
            }
        }
        [CommandMethod("Condition")]
        public void Condition()
        {
            Condition rect = new Condition(new Point3d(100, 100, 0));
            List<Entity> entitiesToDraw = new List<Entity>();
            entitiesToDraw.AddRange(rect.entitiesToDraw);

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                                             OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;

                // Add the new object to the block table record and the transaction
                foreach (Entity currEntity in entitiesToDraw)
                {
                    acBlkTblRec.AppendEntity(currEntity);
                    acTrans.AddNewlyCreatedDBObject(currEntity, true);
                }

                // Save the new line to the database
                acTrans.Commit();
            }
        }
        [CommandMethod("Cycle")]
        public void Cycle()
        {
            Cycle rect = new Cycle(new Point3d(100, 100, 0));
            List<Entity> entitiesToDraw = new List<Entity>();
            entitiesToDraw.AddRange(rect.entitiesToDraw);

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                                             OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;

                // Add the new object to the block table record and the transaction
                foreach (Entity currEntity in entitiesToDraw)
                {
                    acBlkTblRec.AppendEntity(currEntity);
                    acTrans.AddNewlyCreatedDBObject(currEntity, true);
                }

                // Save the new line to the database
                acTrans.Commit();
            }
        }

    }
}
