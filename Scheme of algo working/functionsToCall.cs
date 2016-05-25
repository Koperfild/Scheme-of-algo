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

    public class functionsToCall
    {
        [CommandMethod("DrawFrameWithText")]
        public void DrawFrameWithText()
        {
            InsertFrameFromFile("Маленькая Рамка.dwg");
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
                MText mtext = new MText();
                mtext.Contents = "Some Text for\nSAPR";
                mtext.Location = new Point3d(50, 200, 0);
                mtext.TextHeight = 15;
                acBlkTblRec.AppendEntity(mtext);
                acTrans.AddNewlyCreatedDBObject(mtext, true);


                // Save the new line to the database
                acTrans.Commit();
            }
        }

        [CommandMethod("DrawScheme")]
        public void DrawScheme()
        {
            InsertFrameFromFile("Большая рамка.dwg");

            List<Entity> entitiesToDraw = new List<Entity>();


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
                // Reads data from file, then makes entities from it
                entitiesToDraw = SchemeDraw.getSchemeEntities(acBlkTblRec, acTrans);

                // Add the new object to the block table record and the transaction
                foreach (Entity currEntity in entitiesToDraw)
                {
                    //Because polyline3d is shit and i add it in SchemeDraw.getSchemeEntities method
                    if (currEntity is Polyline3d)
                        continue;
                    acBlkTblRec.AppendEntity(currEntity);
                    acTrans.AddNewlyCreatedDBObject(currEntity, true);
                }

                // Save the new line to the database
                acTrans.Commit();
            }
        }

        
        public void InsertFrameFromFile(string filePath)
        {
            // Get the current database and start a transaction
            Database acCurDb;
            acCurDb = Application.DocumentManager.MdiActiveDocument.Database;


            var id = ObjectId.Null;
            using (var sourceDb = new Database(false, true))
            {
                sourceDb.ReadDwgFile(filePath, System.IO.FileShare.Read, false, null);
                id = acCurDb.Insert("Frame", sourceDb, true);
            }
            using (var tr = acCurDb.TransactionManager.StartTransaction())
            {
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = tr.GetObject(acCurDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                BlockReference br = new BlockReference(Point3d.Origin, id);
                acBlkTblRec.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);
                tr.Commit();
            }
        }
    }
}
/*
 Used this code to read outer file (frame) http://help.autodesk.com/view/ACD/2016/ENU/?guid=GUID-72029044-0840-4187-9A58-F2A4518E3A23
 and this https://forums.autodesk.com/t5/net/enter-dwg-into-current-document/m-p/6343315/highlight/false
 private void InsertModelSpace(string fileName)
        {
            var curDb = HostApplicationServices.WorkingDatabase;
            var id = ObjectId.Null;
            using (var sourceDb = new Database(false, true))
            {
                sourceDb.ReadDwgFile(fileName, System.IO.FileShare.Read, false, null);
                id = curDb.Insert("Test", sourceDb, true);
            }
            using (var tr = curDb.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(
                    SymbolUtilityServices.GetBlockModelSpaceId(curDb), 
                    OpenMode.ForWrite);
                var br = new BlockReference(Point3d.Origin, id);
                ms.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);
                tr.Commit();
            }
        }
        */

