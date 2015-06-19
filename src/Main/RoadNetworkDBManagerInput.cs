using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace USC.GISResearchLab.Common.ShapefileReader
{
  public delegate void DoEventsDelegation();

  public enum SQLVersionEnum
  {
    SQLServer2008,
    SQLServer2005,
    Unknown
  }

  [Serializable]
  public class RoadNetworkDBManagerInput
  {
    public string MasterTableName { get; set; }
    public ShapeFileImporterInput ImporterInput;
    public string LogFileBase;
    public bool LogEnabled;
    public string SQLDataSource { get; set; }
    public string SQLInitialCatalog { get; set; }
    public string SQLUserID { get; set; }
    public SQLVersionEnum SQLVersion { get; set; }
    [XmlIgnore]
    public string SQLPassword { get; set; }
    [XmlIgnore]
    public string DatabaseToModify;
    [XmlIgnore]
    public List<string> DatabasesToMerge;
    [XmlIgnore]
    public TraceSource MyTraceSource;
    [XmlIgnore]
    public DoEventsDelegation DoEventsMethod;

    public string SQLConnectionString
    {
      get
      {
        string conStr = "";
        try
        {
          SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
          builder.DataSource = SQLDataSource;
          builder.InitialCatalog = SQLInitialCatalog;
          builder.Password = SQLPassword;
          builder.UserID = SQLUserID;
          builder.IntegratedSecurity = false;
          conStr = builder.ConnectionString;
        }
        catch
        {
        }
        return conStr;
      }
    }
    /// <summary>
    /// A summery of what this object is holding as an string. Good for log file.
    /// </summary>
    public override string ToString()
    {
      string o = "Database to modify = "+this.DatabaseToModify;
      o += "; Log file enabled = "+this.LogEnabled;
      o += "; Log Files = " + this.LogFileBase + ".log, " + this.LogFileBase + "_bug.log";
      o += "; Master Table Name = "+this.MasterTableName;
      o += "; SQL Data Source = "+this.SQLDataSource;
      o += "; SQL Initial Catalog = "+this.SQLInitialCatalog;
      o += "; SQL User = "+this.SQLUserID;
      o += "; Import Data Description = "+this.ImporterInput.DataDescription;
      o += "; Import Data Year = "+this.ImporterInput.DataYear;
      o += "; Import Data Provider = "+this.ImporterInput.MyDataProvider;
      o += "; Import Database name = "+this.ImporterInput.RoadNetworkDatabaseName;
      o += "; Import Directory = "+this.ImporterInput.RootDirectory;
      o += "; Import Set As Primary = " + this.ImporterInput.SetAsPrimary;
      o += "; SQL Version = " + this.SQLVersion;
      return o;
    }
    /// <summary>
    /// This should be called only when you don't want to deserialize the configuration but instead you want a fresh copy
    /// </summary>
    public void SetToDefault()
    {
      MasterTableName = "AvailableRoadNetworkData";
      SQLDataSource = "sqlserver";
      SQLInitialCatalog = "shortpath";
      SQLUserID = "sa";
      LogFileBase = string.Empty;
      LogEnabled = true;
      this.ImporterInput = new ShapeFileImporterInput();
      this.ImporterInput.MyDataProvider = DataProvider.Navteq;
      this.ImporterInput.RootDirectory = @"c:\";
      DoEventsMethod = null;
      SQLVersion = SQLVersionEnum.Unknown;
    }
    public static RoadNetworkDBManagerInput LoadFormXMLFile(string filename, bool LoadDefaultOnError)
    {
      RoadNetworkDBManagerInput me = null;
      FileStream myFileStream = null;
      XmlSerializer mySerializer = null;
      try
      {
        mySerializer = new XmlSerializer(typeof(RoadNetworkDBManagerInput));
        myFileStream = new FileStream(filename, FileMode.Open);
        me = (RoadNetworkDBManagerInput)(mySerializer.Deserialize(myFileStream));
      }
      catch (Exception ex)
      {
        if (LoadDefaultOnError)
        {
          me = new RoadNetworkDBManagerInput();
          me.SetToDefault();
        }
        else throw new Exception("RoadNetworkDBManagerInput: An error occured during reading/loading the xml configuration file.", ex);
      }
      finally
      {
        if (myFileStream != null) myFileStream.Close();
      }
      return me;
    }
    public void SaveToFile(string filename)
    {
      StreamWriter myWriter = null;
      XmlSerializer mySerializer = null;
      try
      {
        mySerializer = new XmlSerializer(typeof(RoadNetworkDBManagerInput));
        if (File.Exists(filename)) File.SetAttributes(filename, FileAttributes.Normal);
        myWriter = new StreamWriter(filename);
        mySerializer.Serialize(myWriter, this);
      }
      catch (Exception ex)
      {
        throw new Exception("RoadNetworkDBManagerInput: An error occured during saving the xml configuration file.", ex);
      }
      finally
      {
        if (myWriter != null) myWriter.Close();
      }
    }
    public static string CheckSQLConnection(string newConStr)
    {
      string msg = "";
      SqlConnection con = null;
      try
      {
        con = new SqlConnection(newConStr);
        con.Open();
      }
      catch (Exception ex)
      {
        msg = ex.Message;
      }
      finally
      {
        if (con != null) con.Close();
      }
      return msg;
    }
  }
}