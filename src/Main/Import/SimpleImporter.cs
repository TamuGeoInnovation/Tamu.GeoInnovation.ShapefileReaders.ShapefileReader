using Microsoft.SqlServer.Types;
using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using USC.GISResearchLab.Common.ShapeLibs;

namespace USC.GISResearchLab.Common.ShapefileReader
{
    public class SimpleImporter : ShapeFileImporter
    {
        public SimpleImporter()
            : base() { }


        public override DataTable ReadToDataTable(BackgroundWorker MyWorker, string shapefileLocation)
        {
            DataTable ret = new DataTable();
            var shape = new SqlGeography();
            string msg = string.Empty;
            string pmsg = string.Empty;
            IntPtr ptrSHP = IntPtr.Zero;
            IntPtr ptrDBF = IntPtr.Zero;
            DateTime start = DateTime.Now;
            string status = string.Empty;
            try
            {
                double streetLen = 0;
                int nEntities = 0, decimals = 0, fieldWidth = 0, fieldCount = 0, NoOfDBFRec = 0;

                StringBuilder strFieldName = null;
                double[] adfMin = null, adfMax = null, Xarr = null, Yarr = null;

                ShapeLib.ShapeType nShapeType = 0;
                ShapeLib.DBFFieldType fType;
                ShapeLib.DBFFieldType type;
                shape.STSrid = 4326;


                totalFilesBytes = 0;
                totalFilesBytes = (new FileInfo(shapefileLocation)).Length;

                try
                {
                    nEntities = 0;
                    decimals = 0;
                    fieldWidth = 0;
                    streetLen = 0;
                    nShapeType = 0;
                    msg = string.Empty;
                    pmsg = string.Empty;

                    adfMin = new double[2];
                    adfMax = new double[2];
                    Xarr = null; Yarr = null;

                    ptrSHP = IntPtr.Zero;
                    ptrSHP = ShapeLib.SHPOpen(shapefileLocation, "r+b");
                    if ((Marshal.GetLastWin32Error() != 0) && (ptrSHP == IntPtr.Zero))
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), Marshal.GetExceptionPointers());
                    }

                    ShapeLib.SHPGetInfo(ptrSHP, ref nEntities, ref nShapeType, adfMin, adfMax);
                    if ((Marshal.GetLastWin32Error() != 0) && (nShapeType == 0))
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), Marshal.GetExceptionPointers());
                    }

                    fType = ShapeLib.DBFFieldType.FTInvalid;

                    ptrDBF = IntPtr.Zero;
                    ptrDBF = ShapeLib.DBFOpen(shapefileLocation, "r+b");
                    if ((Marshal.GetLastWin32Error() != 0) && (ptrDBF == IntPtr.Zero))
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), Marshal.GetExceptionPointers());
                    }

                    NoOfDBFRec = ShapeLib.DBFGetRecordCount(ptrDBF);
                    if ((Marshal.GetLastWin32Error() != 0) && (NoOfDBFRec < 0))
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), Marshal.GetExceptionPointers());
                    }

                    strFieldName = new StringBuilder(String.Empty);
                    fieldCount = ShapeLib.DBFGetFieldCount(ptrDBF);

                    for (int i = 0; i < fieldCount; i++)
                    {
                        fType = ShapeLib.DBFFieldType.FTInvalid;
                        fType = ShapeLib.DBFGetFieldInfo(ptrDBF, i, strFieldName, ref fieldWidth, ref decimals);
                        if ((Marshal.GetLastWin32Error() != 0) && (fType == ShapeLib.DBFFieldType.FTInvalid))
                        {
                            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), Marshal.GetExceptionPointers());
                        }

                        switch (fType)
                        {
                            case ShapeLib.DBFFieldType.FTDouble:
                                ret.Columns.Add(strFieldName.ToString(), typeof(Decimal));
                                break;
                            case ShapeLib.DBFFieldType.FTInteger:
                                ret.Columns.Add(strFieldName.ToString(), typeof(Int32));
                                break;
                            case ShapeLib.DBFFieldType.FTLogical:
                                ret.Columns.Add(strFieldName.ToString(), typeof(Boolean));
                                break;
                            case ShapeLib.DBFFieldType.FTString:
                                ret.Columns.Add(strFieldName.ToString(), typeof(String));
                                break;
                        }
                    }

                    ret.Columns.Add("shape_X", typeof(Double));
                    ret.Columns.Add("shape_Y", typeof(Double));

                    for (int count = 0; count < nEntities; count++)
                    {

                        ShapeLib.SHPObject shpObj = null;
                        IntPtr pshpObj = IntPtr.Zero;

                        shpObj = new ShapeLib.SHPObject();
                        pshpObj = IntPtr.Zero;
                        pshpObj = ShapeLib.SHPReadObject(ptrSHP, count);
                        shpObj = (ShapeLib.SHPObject)(Marshal.PtrToStructure(pshpObj, typeof(ShapeLib.SHPObject)));

                        if ((Marshal.GetLastWin32Error() != 0) && (pshpObj == IntPtr.Zero))
                        {
                            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), Marshal.GetExceptionPointers());
                        }


                        object[] rowValues = new object[fieldCount + 2];

                        for (int field = 0; field < fieldCount; field++)
                        {
                            type = ShapeLib.DBFGetFieldInfo(ptrDBF, field, strFieldName, ref fieldWidth, ref decimals);
                            switch (type)
                            {
                                case ShapeLib.DBFFieldType.FTDouble:
                                    rowValues[field] = ShapeLib.DBFReadDoubleAttribute(ptrDBF, count, field).ToString();
                                    break;
                                case ShapeLib.DBFFieldType.FTInteger:
                                    rowValues[field] = ShapeLib.DBFReadIntegerAttribute(ptrDBF, count, field).ToString();
                                    break;
                                case ShapeLib.DBFFieldType.FTLogical:
                                    rowValues[field] = ShapeLib.DBFReadLogicalAttribute(ptrDBF, count, field).ToString();
                                    break;
                                case ShapeLib.DBFFieldType.FTString:
                                    rowValues[field] = ShapeLib.DBFReadStringAttribute(ptrDBF, count, field).ToString();
                                    break;
                                case ShapeLib.DBFFieldType.FTInvalid:
                                default:
                                    throw new Exception("Invalid field type in file: " + shapefileLocation);
                            }
                        }

                        Xarr = new double[1];
                        Yarr = new double[1];

                        Marshal.Copy(shpObj.padfX, Xarr, 0, 1);
                        Marshal.Copy(shpObj.padfY, Yarr, 0, 1);

                        rowValues[rowValues.Length - 2] = Xarr[0];
                        rowValues[rowValues.Length - 1] = Yarr[0];

                        ret.Rows.Add(rowValues);


                        //if (pshpObj != IntPtr.Zero)
                        //{
                        //    ShapeLib.SHPDestroyObject(pshpObj);
                        //    pshpObj = IntPtr.Zero;
                        //}
                    }
                }
                catch (Exception ex)
                {
                    msg = DateTime.Now.ToLongTimeString() + ": NavteqImporter SaveToSQL: An error occured during shape file process: " + ex.ToString();
                    ErrorCount++;
                }
                finally
                {

                    if (ptrDBF != IntPtr.Zero)
                    {
                        ShapeLib.DBFClose(ptrDBF); ptrDBF = IntPtr.Zero;
                    }

                    if (ptrSHP != IntPtr.Zero)
                    {
                        ShapeLib.SHPClose(ptrSHP); ptrSHP = IntPtr.Zero;
                    }

                    //GC.Collect();
                    //GC.WaitForPendingFinalizers();
                }

            }
            catch (Exception ex)
            {
                msg = DateTime.Now.ToLongTimeString() + ": NavteqImporter SaveToSQL: An error occured during process: " + ex.ToString();
                ErrorCount++;
            }
            finally
            {
                if (ptrDBF != IntPtr.Zero)
                {
                    ShapeLib.DBFClose(ptrDBF); ptrDBF = IntPtr.Zero;
                }

                if (ptrSHP != IntPtr.Zero)
                {
                    ShapeLib.SHPClose(ptrSHP); ptrSHP = IntPtr.Zero;
                }

                //GC.Collect();
                //GC.WaitForPendingFinalizers();
            }
            return ret;
        }

        public override ShapeFileImporterOutput SaveToSQL(BackgroundWorker worker, RoadNetworkDBManagerInput e)
        {
            throw new NotImplementedException();
        }

        public override ShapeFileImporterOutput SaveToDisk(BackgroundWorker worker, RoadNetworkDBManagerInput e)
        {
            throw new NotImplementedException();
        }

    }
}