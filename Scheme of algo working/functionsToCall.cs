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
        [CommandMethod("DrawFrame")]
        public void DrawFrame()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            Database FrameDb = new Database(false, true);

            FrameDb.ReadDwgFile("BigFrame.dwg",
                            System.IO.FileShare.Read,
                            true,
                            "");
            // Create a variable to store the list of block identifiers
            ObjectIdCollection blockIds = new ObjectIdCollection();

            Autodesk.AutoCAD.DatabaseServices.TransactionManager tm =
          FrameDb.TransactionManager;
            //insert frame from file
            using (Transaction myT = tm.StartTransaction())
            {
                // Open the block table
                BlockTable bt =
                    (BlockTable)tm.GetObject(FrameDb.BlockTableId,
                                            OpenMode.ForRead,
                                            false);

                // Check each block in the block table
                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr =
                      (BlockTableRecord)tm.GetObject(btrId,
                                                    OpenMode.ForRead,
                                                    false);
                    if (!btr.IsLayout)
                        blockIds.Add(btrId);

                    //btr.Dispose();
                }
                myT.Commit();
            }
            // Copy blocks from source to destination database
            IdMapping mapping = new IdMapping();
            FrameDb.WblockCloneObjects(blockIds,
                                        acCurDb.BlockTableId,
                                        mapping,
                                        DuplicateRecordCloning.Replace,
                                        false);
            FrameDb.Dispose();
        }





        [CommandMethod("DrawScheme")]
        public void DrawScheme()
        {
            InsertFrameFromFile();

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

        
        public void InsertFrameFromFile()
        {
            // Get the current database and start a transaction
            Database acCurDb;
            acCurDb = Application.DocumentManager.MdiActiveDocument.Database;


            var id = ObjectId.Null;
            using (var sourceDb = new Database(false, true))
            {
                sourceDb.ReadDwgFile("BigFrame.dwg", System.IO.FileShare.Read, false, null);
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



            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Create a reference to a DWG file
                string PathName = "C:\\AutoCAD\\Sample\\Sheet Sets\\Architectural\\Res\\Exterior Elevations.dwg";
                ObjectId acXrefId = acCurDb.AttachXref(PathName, "Exterior Elevations");

                // If a valid reference is created then continue
                if (!acXrefId.IsNull)
                {
                    // Attach the DWG reference to the current space
                    Point3d insPt = new Point3d(1, 1, 0);
                    using (BlockReference acBlkRef = new BlockReference(insPt, acXrefId))
                    {
                        BlockTableRecord acBlkTblRec;
                        acBlkTblRec = acTrans.GetObject(acCurDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                        acBlkTblRec.AppendEntity(acBlkRef);
                        acTrans.AddNewlyCreatedDBObject(acBlkRef, true);

                        Application.ShowAlertDialog("The external reference is attached.");

                        Matrix3d mat = acBlkRef.BlockTransform;
                        mat.Inverse();

                        Point2dCollection ptCol = new Point2dCollection();

                        // Define the first corner of the clipping boundary
                        Point3d pt3d = new Point3d(-330, 400, 0);
                        pt3d.TransformBy(mat);
                        ptCol.Add(new Point2d(pt3d.X, pt3d.Y));

                        // Define the second corner of the clipping boundary
                        pt3d = new Point3d(1320, 1120, 0);
                        pt3d.TransformBy(mat);
                        ptCol.Add(new Point2d(pt3d.X, pt3d.Y));

                        // Define the normal and elevation for the clipping boundary 
                        Vector3d normal;
                        double elev = 0;

                        if (acCurDb.TileMode == true)
                        {
                            normal = acCurDb.Ucsxdir.CrossProduct(acCurDb.Ucsydir);
                            elev = acCurDb.Elevation;
                        }
                        else
                        {
                            normal = acCurDb.Pucsxdir.CrossProduct(acCurDb.Pucsydir);
                            elev = acCurDb.Pelevation;
                        }

                        // Set the clipping boundary and enable it
                        using (Autodesk.AutoCAD.DatabaseServices.Filters.SpatialFilter filter =
                            new Autodesk.AutoCAD.DatabaseServices.Filters.SpatialFilter())
                        {
                            Autodesk.AutoCAD.DatabaseServices.Filters.SpatialFilterDefinition filterDef =
                                new Autodesk.AutoCAD.DatabaseServices.Filters.SpatialFilterDefinition(ptCol, normal, elev, 0, 0, true);
                            filter.Definition = filterDef;

                            // Define the name of the extension dictionary and entry name
                            string dictName = "ACAD_FILTER";
                            string spName = "SPATIAL";

                            // Check to see if the Extension Dictionary exists, if not create it
                            if (acBlkRef.ExtensionDictionary.IsNull)
                            {
                                acBlkRef.UpgradeOpen();
                                acBlkRef.CreateExtensionDictionary();
                                acBlkRef.DowngradeOpen();
                            }

                            // Open the Extension Dictionary for write
                            DBDictionary extDict = acTrans.GetObject(acBlkRef.ExtensionDictionary, OpenMode.ForWrite) as DBDictionary;

                            // Check to see if the dictionary for clipped boundaries exists, 
                            // and add the spatial filter to the dictionary
                            if (extDict.Contains(dictName))
                            {
                                DBDictionary filterDict = acTrans.GetObject(extDict.GetAt(dictName), OpenMode.ForWrite) as DBDictionary;

                                if (filterDict.Contains(spName))
                                {
                                    filterDict.Remove(spName);
                                }

                                filterDict.SetAt(spName, filter);
                            }
                            else
                            {
                                using (DBDictionary filterDict = new DBDictionary())
                                {
                                    extDict.SetAt(dictName, filterDict);

                                    acTrans.AddNewlyCreatedDBObject(filterDict, true);
                                    filterDict.SetAt(spName, filter);
                                }
                            }

                            // Append the spatial filter to the drawing
                            acTrans.AddNewlyCreatedDBObject(filter, true);
                        }
                    }

                    Application.ShowAlertDialog("The external reference is clipped.");
                }

                // Save the new objects to the database
                acTrans.Commit();

                // Dispose of the transaction
            }
        }
    }
}

    */