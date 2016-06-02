using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Web;

namespace CreateLicence
{
    public class Generatekey
    {
        string strConn = "Data Source=172.99.97.195,4120;Initial Catalog=513355_EERegistrationV8;User ID=513355_EERegistrationV8;Password=Universal1";
        Hashtable compHash = new Hashtable();
        Hashtable minorAttrHashTable = new Hashtable();
        DataTable majorGridTable = new DataTable();
        string txt_clientToken = string.Empty;
        Dictionary<string, object> session = new Dictionary<string, object>();
        public ValidationException PageLoadCreateKey(RequestInfo details)
        {
            ValidationException validate = LicenseServerValidation(details);
            //bool check = CheckDriveIDExsists(details);
            if(validate.Result)
            {

                try
                {
                    #region Build Minor Attribute Hash Table
                    string strQuery = "select * from sbt_attr_master where attr_type_id = 2";
                    SqlConnection sqlConn = new SqlConnection(strConn);
                    SqlCommand sqlcommand = new SqlCommand(strQuery, sqlConn);
                    sqlcommand.CommandType = CommandType.Text;
                    sqlConn.Open();
                    SqlDataReader dr = sqlcommand.ExecuteReader();
                    while (dr.Read())
                    {
                        minorAttrHashTable.Add(dr["id"].ToString(), dr["attr_name"].ToString());
                    }
                    sqlConn.Close();
                    #endregion

                    //XmlDocument serxDoc = new XmlDocument();
                    //if (session.ContainsKey("SerialXML"))
                    //{
                    //    serxDoc = (XmlDocument)session["SerialXML"];
                    //}

                    //XmlDocument docNew = new XmlDocument();
                    //XmlElement newRoot = docNew.CreateElement("Product");
                    //docNew.AppendChild(newRoot);
                    //newRoot.InnerXml = serxDoc.DocumentElement.InnerXml;
                    //if (session.ContainsKey("ProductXML"))
                    //{
                    //    session["ProductXML"] = docNew;
                    //}
                    //else
                    //{
                    //    session.Add("ProductXML", docNew);
                    //}

                    //HttpContext.Current.Session["ProductXML"] = docNew;

                    XmlDocument xDoc = new XmlDocument();
                    if (session.ContainsKey("ProductXML"))
                    {
                        xDoc = (XmlDocument)session["ProductXML"];
                    }
                    else
                    {
                        xDoc = null;
                    }

                    if (session.ContainsKey("ProductList"))
                    {
                        session["ProductList"] = null;
                    }
                    else
                    {
                        session.Add("ProductList", null);
                    }

                    if (session.ContainsKey("ComponentList"))
                    {
                        session["ComponentList"] = null;
                    }
                    else
                    {
                        session.Add("ComponentList", null);
                    }

                    majorGridTable.Columns.Add("attr_name");
                    majorGridTable.Columns.Add("id");
                    if (xDoc != null)
                    {

                        XmlNodeList MajorCompNodeList = xDoc.SelectNodes("/Product/MajorComponent");

                        //DataTable mytable = new DataTable();


                        foreach (XmlNode xmlnode in MajorCompNodeList)
                        {
                            majorGridTable.Rows.Add(xmlnode.SelectSingleNode("MajorComponentName").InnerText, xmlnode.SelectSingleNode("MajorComponentID").InnerText);
                        }


                        XmlNodeList CompNodeList = xDoc.SelectNodes("/Product/MajorComponent/Components/ComponentID");
                        ArrayList componentList = new ArrayList();
                        foreach (XmlNode compnode in CompNodeList)
                        {
                            componentList.Add(compnode.InnerText);
                        }
                        //HttpContext.Current.Session["ComponentList"] = componentList;
                        if (session.ContainsKey("ComponentList"))
                        {
                            session["ComponentList"] = componentList;
                        }
                        else
                        {
                            session.Add("ComponentList", componentList);
                        }


                        XmlNodeList MetricNodeList = xDoc.SelectNodes("/Product/MajorComponent/Metrics/Metric");
                        ArrayList metricIDList = new ArrayList();
                        ArrayList metricValueList = new ArrayList();
                        foreach (XmlNode MetricNode in MetricNodeList)
                        {
                            if (MetricNode.SelectSingleNode("ParentID").InnerText != "")
                            {
                                string ParentID = MetricNode.SelectSingleNode("ParentID").InnerText;
                                if (metricIDList.Contains(ParentID))
                                {
                                    foreach (XmlNode ParentMetricNode in MetricNodeList)
                                    {
                                        if (ParentMetricNode.SelectSingleNode("MetricID").InnerText == ParentID)
                                        {
                                            metricIDList.Add(MetricNode.SelectSingleNode("MetricID").InnerText);
                                            string ComplexAttribute = ParentMetricNode.SelectSingleNode("MetricComplexAttr").InnerText;
                                            string ComplexValue = ParentMetricNode.SelectSingleNode("MetricComplexValue").InnerText;

                                            if (ComplexAttribute == "Hour")
                                            {
                                                metricValueList.Add(ComplexValue + ":h");
                                            }
                                            else if (ComplexAttribute == "Day")
                                            {
                                                int intComplex = 0;
                                                if (ComplexValue != "")
                                                    intComplex = 24 * Convert.ToInt32(ComplexValue);
                                                metricValueList.Add(intComplex + ":d");
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                metricIDList.Add(MetricNode.SelectSingleNode("MetricID").InnerText);
                                metricValueList.Add(MetricNode.SelectSingleNode("MetricValue").InnerText);
                            }
                        }

                        //HttpContext.Current.Session["MetricIDList"] = metricIDList;
                        //HttpContext.Current.Session["MetricValueList"] = metricValueList;

                        if (session.ContainsKey("MetricIDList"))
                        {
                            session["MetricIDList"] = metricIDList;
                        }
                        else
                        {
                            session.Add("MetricIDList", metricIDList);
                        }

                        if (session.ContainsKey("MetricValueList"))
                        {
                            session["MetricValueList"] = metricValueList;
                        }
                        else
                        {
                            session.Add("MetricValueList", metricValueList);
                        }
                    }
                    validate.NewKey = CreateKey(details);
                    return validate;
                }
                catch(Exception)
                {
                    validate.NewKey = string.Empty;
                    return validate;
                }
            }
            else
            {
                validate.NewKey = string.Empty;
                return validate;
            }
        }

        

        public ValidationException LicenseServerValidation(RequestInfo details)
        {
            ValidationException validate = new ValidationException();

            //Validate Email
            IsEmailValid(details.Email, validate);
            if (!validate.Result) return validate;
            

            //validate Email Domain exceptions--remove gmail.hotmail and yahoo
            ValidateEmailDomain(details.Email, validate);
            if (!validate.Result) return validate;

            //IsEmailExsists
            CheckEmailExsists(details, validate);
            if (!validate.Result) return validate;

            ////Validate driveid
            //CheckDriveIDExsists(details, validate);
            //if (!validate.Result) return validate;

            validate.validateMessage = "Thank you for your request. Your 30-day trial key has been sent to the email address provided: " + details.Email;

            return validate;

        }

        public void CheckEmailExsists(RequestInfo details, ValidationException validate)
        {
            SqlConnection sqlConn = new SqlConnection(strConn);
            try
            {
                string strQuery = "select Count([Email_]) from sbt_customer where [Email_] = '" + details.Email + "'";

                SqlCommand sqlcommand = new SqlCommand(strQuery, sqlConn);
                sqlcommand.CommandType = CommandType.Text;
                sqlConn.Open();
                Int32 count = (Int32)sqlcommand.ExecuteScalar();

                if (details.Email == "development@stonebond.com")
                {
                    count = 0;
                }
                if (count > 0)
                {
                    validate.Result = false;
                    validate.validateMessage = "This email address has already received a 30-day trial key. Please provide an alternative email or contact demo@stonebond.com";
                }
            }
            catch (Exception)
            {
                validate.Result = false;
                validate.validateMessage = "This email address has already received a 30-day trial key. Please provide an alternative email or contact demo@stonebond.com";
            }
            finally
            {
                sqlConn.Close();
            }
              
        }

        public void ValidateEmailDomain(string email, ValidationException validate)
        {
            string emailaddress = email.Substring(email.IndexOf('@') + 1, email.IndexOf('.') - email.IndexOf('@') - 1);
            if(emailaddress=="gmail" || emailaddress=="hotmail" || emailaddress=="yahoo")
            {
                validate.Result = false;
                validate.validateMessage = "Cannot create a license key using personal email address. Please provide a business email or contact demo@stonebond.com";
            }
        }

        public void IsEmailValid(string emailaddress, ValidationException validate)
        {
            try
            {
                MailAddress m = new MailAddress(emailaddress);
                validate.Result= true;
                
            }
            catch (FormatException)
            {
                validate.Result= false;
                validate.validateMessage = "Invalid Email Address. Please check the email again.";
            }
        }

        public void CheckDriveIDExsists(RequestInfo details,ValidationException validate)
        {
            SqlConnection sqlConn = new SqlConnection(strConn);
            try
            {
                string strQuery = "select Count([driveId]) from sbt_customer where [driveId] = '" + details.DriveID + "'";
                
                SqlCommand sqlcommand = new SqlCommand(strQuery, sqlConn);
                sqlcommand.CommandType = CommandType.Text;
                sqlConn.Open();
                Int32 count = (Int32)sqlcommand.ExecuteScalar(); 

                if (count > 0)
                {
                    validate.Result=false;
                    validate.validateMessage = "Key Already installed on this machine earlier";
                }
            }
           catch(Exception)
            {
                validate.Result = false;
                validate.validateMessage = "Key Already installed on this machine earlier";
            }
            finally
            {
                sqlConn.Close();
            }
              
        }

        public string CreateKey(RequestInfo details)
        {
            string newKeys=string.Empty;

            #region "Add To License Event"

            if (!HasConflicts())
            {
                QueryMajorComponents();
            }

            #endregion "Add To License Event"

            txt_clientToken = Guid.NewGuid().ToString();
            SaveXML();
            if (ValidateClientToken())
            {
                newKeys= Update(details);
                //SendEmail(details,newKeys); Moved to EE send email node
            }
            else
            {
                //lbl_message.Text = "Please generate or enter a Client Token";
                //lbl_message.Visible = true;
            }
            return newKeys;
        }

        protected bool HasConflicts()
        {
            Hashtable metricHash = new Hashtable();
            ArrayList duplicates = new ArrayList();

            string strMajorCompQuery = "SELECT a.MajorComponentID, a.MetricID, b.attr_name, a.value FROM SBT_PRODUCT_ATTRIBUTES a , sbt_attr_master b WHERE a.MetricID = b.id and a.value != '-1' and a.product_id = 193"; //hard coded for all features training
            SqlConnection sqlMajorCompConn = new SqlConnection(strConn);
            SqlCommand sqlMajorCompcommand = new SqlCommand(strMajorCompQuery, sqlMajorCompConn);
            sqlMajorCompcommand.CommandType = CommandType.Text;
            sqlMajorCompConn.Open();
            SqlDataReader proddr = sqlMajorCompcommand.ExecuteReader();


            while (proddr.Read())
            {
                if (proddr["MetricID"].ToString() != "19")
                {
                    if (!metricHash.Contains(proddr["MajorComponentID"].ToString() + "|" + proddr["MetricID"].ToString()))
                        metricHash.Add(proddr["MajorComponentID"].ToString() + "|" + proddr["MetricID"].ToString(), "Training All Features|" + proddr["value"].ToString());
                    else
                        duplicates.Add(proddr["MajorComponentID"].ToString() + "|" + proddr["MetricID"].ToString() + "|" + proddr["attr_name"].ToString()
                            + "|Training All Features|" + proddr["value"].ToString());
                }
            }
            sqlMajorCompConn.Close();

            XmlDocument xDoc = new XmlDocument();
            if (session.ContainsKey("ProductXML"))
            {
                xDoc = (XmlDocument)session["ProductXML"];
            }
            else
            {
                xDoc = null;
            }

            if (xDoc != null)
            {
                XmlNodeList MajorCompNodeList = xDoc.SelectNodes("/Product/MajorComponent");
                foreach (XmlNode compnode in MajorCompNodeList)
                {
                    string strComponentID = compnode.SelectSingleNode("MajorComponentID").InnerText;
                    XmlNodeList metricList = compnode.SelectNodes("Metrics/Metric");
                    foreach (XmlNode metricNode in metricList)
                    {
                        if (metricNode.SelectSingleNode("Selected").InnerText == "True")
                        {
                            if (metricNode.SelectSingleNode("MetricID").InnerText != "19")
                            {
                                string strParentID = metricNode.SelectSingleNode("ParentID").InnerText;
                                if (strParentID == "" || strParentID == string.Empty || strParentID == null)
                                {
                                    if (!metricHash.Contains(strComponentID + "|" + metricNode.SelectSingleNode("MetricID").InnerText))
                                        metricHash.Add(strComponentID + "|" + metricNode.SelectSingleNode("MetricID").InnerText, metricNode.SelectSingleNode("MetricName").InnerText
                                            + "|" + metricNode.SelectSingleNode("MetricValue").InnerText);
                                    else
                                        duplicates.Add(strComponentID + "|" + metricNode.SelectSingleNode("MetricID").InnerText + "|" + metricNode.SelectSingleNode("MetricName").InnerText
                                            + "|Currently Picked|" + metricNode.SelectSingleNode("MetricValue").InnerText);
                                }
                                else
                                {
                                    foreach (XmlNode parentNode in metricList)
                                    {
                                        if (parentNode.SelectSingleNode("MetricID").InnerText == strParentID)
                                        {
                                            string ComplexValue = parentNode.SelectSingleNode("MetricComplexValue").InnerText;
                                            string ComplexAttribute = parentNode.SelectSingleNode("MetricComplexAttr").InnerText;
                                            int Hours = 1;
                                            string prefix = string.Empty;
                                            if (ComplexAttribute == "Hour")
                                            {
                                                Hours = 1;
                                                prefix = ":h";
                                            }
                                            else if (ComplexAttribute == "Day")
                                            {
                                                Hours = 24;
                                                prefix = ":d";
                                            }
                                            int ComplexMetricValue = Hours * Convert.ToInt32(ComplexValue);


                                            if (!metricHash.Contains(strComponentID + "|" + metricNode.SelectSingleNode("MetricID").InnerText))
                                                metricHash.Add(strComponentID + "|" + metricNode.SelectSingleNode("MetricID").InnerText, metricNode.SelectSingleNode("MetricName").InnerText
                                                    + "|" + ComplexMetricValue.ToString() + prefix);
                                            else
                                                duplicates.Add(strComponentID + "|" + metricNode.SelectSingleNode("MetricID").InnerText + "|" + metricNode.SelectSingleNode("MetricName").InnerText
                                                    + "|Currently Picked|" + ComplexMetricValue.ToString() + prefix);

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //HttpContext.Current.Session["OriginalMetrics"] = metricHash;
            if (session.ContainsKey("OriginalMetrics"))
            {
                session["OriginalMetrics"] = metricHash;
            }
            else
            {
                session.Add("OriginalMetrics", metricHash);
            }


            if (duplicates.Count > 0)
            {
                //ViewConflictsLink.Attributes["href"] = "#";
                //ViewConflictsLink.Attributes["onclick"] = String.Format("return ShowEditForm();");
                //ViewConflictsLink.Visible = true;
                //btn_Refresh.Visible = true;
                //HttpContext.Current.Session["DuplicateMetrics"] = duplicates;
                if (session.ContainsKey("DuplicateMetrics"))
                {
                    session["DuplicateMetrics"] = duplicates;
                }
                else
                {
                    session.Add("DuplicateMetrics", duplicates);
                }
                return true;
            }

            return false;
        }

        protected void QueryMajorComponents()
        {
            DataTable mytable = new DataTable();
            mytable.Columns.Add("attr_name");
            mytable.Columns.Add("id");


            foreach (DataRow dr in majorGridTable.Rows)
            {
                mytable.Rows.Add(dr["attr_name"].ToString(), dr["id"].ToString());
                compHash.Add(dr["id"].ToString(), dr["attr_name"].ToString());
            }
            ArrayList productList = new ArrayList();

            #region Query Major Components
            productList.Add("193");
            string strMajorCompQuery = "SELECT DISTINCT a.MajorComponentID, b.attr_name FROM SBT_PRODUCT_COMPONENTS a , sbt_attr_master b WHERE a.MajorComponentID = b.id and a.ProductId = 193";
            SqlConnection sqlMajorCompConn = new SqlConnection(strConn);
            SqlCommand sqlMajorCompcommand = new SqlCommand(strMajorCompQuery, sqlMajorCompConn);
            sqlMajorCompcommand.CommandType = CommandType.Text;
            sqlMajorCompConn.Open();
            SqlDataReader proddr = sqlMajorCompcommand.ExecuteReader();
            bool isNew;

            while (proddr.Read())
            {
                isNew = false;
                if (!(compHash.Contains(proddr["MajorComponentID"].ToString())))
                {
                    mytable.Rows.Add(proddr["attr_name"].ToString(), proddr["MajorComponentID"].ToString());
                    compHash.Add(proddr["MajorComponentID"].ToString(), proddr["attr_name"].ToString());
                    isNew = true;
                }
                QueryComponents("193", proddr["MajorComponentID"].ToString(), proddr["attr_name"].ToString(), isNew);
                QueryMetrics(proddr["MajorComponentID"].ToString(), "193");
            }
            sqlMajorCompConn.Close();


            #endregion

            majorGridTable.Rows.Clear();
            foreach (DataRow dr in mytable.Rows)
            {
                DataRow reBindRows = majorGridTable.NewRow();
                reBindRows["attr_name"] = dr["attr_name"];
                reBindRows["id"] = dr["id"];
                majorGridTable.Rows.Add(reBindRows);
            }
            ////MajorCompGrid.DataSource = mytable;
            ////MajorCompGrid.Rebind();
            //HttpContext.Current.Session["ProductList"] = productList;
            if (session.ContainsKey("ProductList"))
            {
                session["ProductList"] = productList;
            }
            else
            {
                session.Add("ProductList", productList);
            }
        }

        private void SaveXML()
        {
            XmlDocument xmldoc = new XmlDocument();
            XmlElement tempNode = xmldoc.CreateElement("License");
            xmldoc.AppendChild(tempNode);
            XmlNode parentnode = xmldoc.ChildNodes[0];
            foreach (DataRow dr in majorGridTable.Rows)
            {
                tempNode = xmldoc.CreateElement("MajorComponent");
                parentnode.AppendChild(tempNode);
                XmlNode majorCompNode = tempNode;
                tempNode = xmldoc.CreateElement("MajorComponentID");
                tempNode.InnerText = dr["id"].ToString();
                majorCompNode.AppendChild(tempNode);
                tempNode = xmldoc.CreateElement("MajorComponentName");
                tempNode.InnerText = dr["attr_name"].ToString();
                majorCompNode.AppendChild(tempNode);

                ArrayList selectedMetrics = new ArrayList();
                XmlDocument metricDoc = (XmlDocument)session[dr["id"].ToString() + "Metrics"];
                XmlNode metricNode = metricDoc.SelectSingleNode("/Metrics");
                if (metricNode != null)
                {
                    majorCompNode.AppendChild(xmldoc.ImportNode(metricNode, true));
                }

                #region Code for adding Components

                ArrayList arylist = (ArrayList)(session[dr["id"].ToString() + "DestinationComponents"]);
                foreach (RadListBoxItem item in arylist)
                {
                    tempNode = xmldoc.CreateElement("Components");
                    majorCompNode.AppendChild(tempNode);
                    XmlNode compnode = tempNode;
                    XmlElement x4 = xmldoc.CreateElement("ComponentAttributeName");
                    x4.InnerText = item.Text;
                    compnode.AppendChild(x4);
                    string[] componentsIDs = null;
                    if (item.DataItem != null)
                    {
                        componentsIDs = item.DataItem.Split('/');

                        x4 = xmldoc.CreateElement("ComponentID");
                        x4.InnerText = componentsIDs[0];
                        compnode.AppendChild(x4);
                        x4 = xmldoc.CreateElement("ComponentAttributeID");
                        x4.InnerText = componentsIDs[1];
                        compnode.AppendChild(x4);

                    }
                    else
                    {
                        string[] attrName = item.Text.Split('/');
                        SqlConnection conn = new SqlConnection(strConn);
                        conn.Open();
                        string strSQLCommand = "SELECT a.id, b.id FROM [sbt_components]a , [sbt_attr_master] b where a.componentName = '" + attrName[0] + "' and b.attr_name = '" + attrName[1] + "'";
                        SqlCommand command = new SqlCommand(strSQLCommand, conn);
                        SqlDataReader sdr = command.ExecuteReader();
                        string ComponentID = string.Empty;
                        string AttributeID = string.Empty;
                        while (sdr.Read())
                        {
                            ComponentID = sdr[0].ToString();
                            AttributeID = sdr[1].ToString();
                        }
                        sdr.Close();
                        conn.Close();
                        x4 = xmldoc.CreateElement("ComponentID");
                        x4.InnerText = ComponentID;
                        compnode.AppendChild(x4);
                        x4 = xmldoc.CreateElement("ComponentAttributeID");
                        x4.InnerText = AttributeID;
                        compnode.AppendChild(x4);

                    }

                }
                #endregion

            }
            //HttpContext.Current.Session["License"] = xmldoc;
            if (session.ContainsKey("License"))
            {
                session["License"] = xmldoc;
            }
            else
            {
                session.Add("License", xmldoc);
            }
        }

        protected bool ValidateClientToken()
        {
            bool returnValue = true;
            XmlDocument licDoc = (XmlDocument)session["License"];
            XmlNodeList metricList = licDoc.SelectNodes("License/MajorComponent/Metrics/Metric/MetricID");
            foreach (XmlNode metricNode in metricList)
            {
                if (metricNode.InnerText == "19" && txt_clientToken== "")
                {
                    XmlNode parentNode = metricNode.ParentNode;
                    XmlNode isSelectedNode = parentNode.SelectSingleNode("Selected");
                    if (isSelectedNode.InnerText == "True")
                        returnValue = false;
                }
            }
            return returnValue;
        }

        private string Update(RequestInfo details)
        {
            string newKeys = string.Empty;
            #region Update serial table

            XmlDocument xdoc = new XmlDocument();
            xdoc = (XmlDocument)session["License"];
            SqlConnection sqlConn = new SqlConnection(strConn);
            string strDomainValue = "mtc-sbc\\";
            string strCreatedBy = details.FirstName;
            //string strCreatedBy = Session["Username"].ToString();
            string strSerialNumber = string.Empty;
            int Duration = 30; //hardcoded for free trial
            string strOrganizationID = "46";

            int NumofKeys = 1; //only 1 key for free trial version
            int keyIndex = 0;
            try
            {
                string strIsServer = "1";
                int IsTrialValid = 1;
                int InternalUse = 1;
                string strResellerID = "1"; //reseller id for StoneBond technologies



                //New Code with Multiple keys generation at once
                //Author: Siddharth Chandwani
                SqlCommand command = new SqlCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "sp_InsertSerialComponentsInfo";
                command.Connection = sqlConn;

                //loop for n number of keys to be generated.
                for (int i = 0; i < NumofKeys; i++)
                {
                    keyIndex = i + 1;
                    strSerialNumber = Guid.NewGuid().ToString();

                    command.Parameters.AddWithValue("@serialNumber", strSerialNumber);
                    command.Parameters.AddWithValue("@licenseTypeId", 1);
                    command.Parameters.AddWithValue("@description","Free Trial Evaluation - Key " + DateTime.Now);
                    command.Parameters.AddWithValue("@isServer", strIsServer);
                    command.Parameters.AddWithValue("@createdBy", strCreatedBy);
                    command.Parameters.AddWithValue("@duration", Duration);
                    command.Parameters.AddWithValue("@IsTrialValid", IsTrialValid);
                    command.Parameters.AddWithValue("@ResellerID", strResellerID);
                    command.Parameters.AddWithValue("@ForInternalUse", InternalUse);
                    sqlConn.Open();
                    command.ExecuteNonQuery();
                    command.Parameters.Clear();
                    sqlConn.Close();
                    newKeys = strSerialNumber;
                }
                //lbl_message.Text = "The License Key is/are generated, please check list box below ";

            }
            catch (Exception)
            {
                
            }
            finally
            {
                sqlConn.Close();
            }

            
            #endregion

            #region Update attributes table

            XmlNodeList majorComponentList = xdoc.SelectNodes("License/MajorComponent");

            for (int i = 1; i <= majorComponentList.Count; i++)
            {
                int metricCount = xdoc.SelectNodes("License/MajorComponent[" + i + "]/Metrics/Metric").Count;
                int majorCompID = Convert.ToInt32(xdoc.SelectSingleNode("License/MajorComponent[" + i + "]/MajorComponentID").InnerText);
                List<string> metricIDList = new List<string>();
                List<string> metricValueList = new List<string>();

                for (int j = 1; j <= metricCount; j++)
                {
                    if (xdoc.SelectSingleNode("License/MajorComponent[" + i + "]/Metrics/Metric[" + j + "]/Selected").InnerText == "True")
                    {

                        metricIDList.Add(xdoc.SelectSingleNode("License/MajorComponent[" + i + "]/Metrics/Metric[" + j + "]/MetricID").InnerText);
                        if (xdoc.SelectSingleNode("License/MajorComponent[" + i + "]/Metrics/Metric[" + j + "]/MetricID").InnerText == "19")
                            metricValueList.Add(txt_clientToken);
                        else
                        {
                            if (xdoc.SelectSingleNode("License/MajorComponent[" + i + "]/Metrics/Metric[" + j + "]/MetricValue").InnerText != "")
                            {
                                metricValueList.Add(xdoc.SelectSingleNode("License/MajorComponent[" + i + "]/Metrics/Metric[" + j + "]/MetricValue").InnerText);
                            }
                            else if (xdoc.SelectSingleNode("License/MajorComponent[" + i + "]/Metrics/Metric[" + j + "]/ParentID").InnerText != string.Empty)
                            {
                                string strParent = xdoc.SelectSingleNode("License/MajorComponent[" + i + "]/Metrics/Metric[" + j + "]/ParentID").InnerText;
                                string strMetric = xdoc.SelectSingleNode("License/MajorComponent[" + i + "]/Metrics/Metric[" + j + "]/MetricID").InnerText;
                                string ComplexValue = string.Empty;
                                string ComplexAttribute = string.Empty;
                                for (int a = 1; a <= metricCount; a++)
                                {
                                    if (xdoc.SelectSingleNode("License/MajorComponent[" + i + "]/Metrics/Metric[" + a + "]/MetricID").InnerText == strParent)
                                    {
                                        //ComplexValue = xdoc.SelectSingleNode("License/MajorComponent[" + i + "]/Metrics/Metric[" + a + "]/MetricValue").InnerText;
                                        ComplexValue = xdoc.SelectSingleNode("License/MajorComponent[" + i + "]/Metrics/Metric[" + a + "]/MetricComplexValue").InnerText;
                                        ComplexAttribute = xdoc.SelectSingleNode("License/MajorComponent[" + i + "]/Metrics/Metric[" + a + "]/MetricComplexAttr").InnerText;
                                        break;
                                    }
                                }
                                int Hours = 1;
                                string prefix = string.Empty;
                                if (ComplexAttribute == "Hour")
                                {
                                    Hours = 1;
                                    prefix = ":h";
                                }
                                else if (ComplexAttribute == "Day")
                                {
                                    Hours = 24;
                                    prefix = ":d";
                                }
                                int ComplexMetricValue = Hours * Convert.ToInt32(ComplexValue);
                                //metricIDList.Add(strMetric);
                                metricValueList.Add(ComplexMetricValue.ToString() + prefix);
                                //metricValueList.Add(ComplexValue);
                            }
                        }
                    }
                }

                int compCount = xdoc.SelectNodes("License/MajorComponent[" + i + "]/Components").Count;
                List<string> compIDList = new List<string>();
                List<string> compAttrIDList = new List<string>();
                for (int j = 1; j <= compCount; j++)
                {
                    compIDList.Add(xdoc.SelectSingleNode("License/MajorComponent[" + i + "]/Components[" + j + "]/ComponentID").InnerText);
                    compAttrIDList.Add(xdoc.SelectSingleNode("License/MajorComponent[" + i + "]/Components[" + j + "]/ComponentAttributeID").InnerText);
                }

                string strMetricID = string.Join(",", metricIDList.Select(x => x.ToString()).ToArray());
                string strMetricValue = string.Join(",", metricValueList.Select(x => x.ToString()).ToArray());
                string strCompID = string.Join(",", compIDList.Select(x => x.ToString()).ToArray());
                string strCompAttrID = string.Join(",", compAttrIDList.Select(x => x.ToString()).ToArray());

                SqlCommand sqlcommand = new SqlCommand();
                sqlcommand.CommandType = CommandType.StoredProcedure;
                sqlcommand.CommandText = "sp_InsertSerialAttributeInfo";
                sqlcommand.Connection = sqlConn;
                //foreach (RadListBoxItem serial in NewKeyListBox.Items)
                //{
                    sqlcommand.Parameters.AddWithValue("@serialNumber", newKeys);
                    sqlcommand.Parameters.AddWithValue("@MajorComponentID", majorCompID);
                    sqlcommand.Parameters.AddWithValue("@durationinDays", Duration);
                    sqlcommand.Parameters.AddWithValue("@description", "Free Trial Evaluation Key" + DateTime.Now);
                    sqlcommand.Parameters.AddWithValue("@createdBy", strCreatedBy);
                    sqlcommand.Parameters.AddWithValue("@MetricIDList", strMetricID);
                    sqlcommand.Parameters.AddWithValue("@MetricValueList", strMetricValue);
                    sqlcommand.Parameters.AddWithValue("@CompIDList", strCompID);
                    sqlcommand.Parameters.AddWithValue("@CompAttrIDList", strCompAttrID);
                    sqlConn.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcommand.Parameters.Clear();
                    sqlConn.Close();
                //}

            }


            #endregion

            #region Update serial product table
            ArrayList componentList = null;
            ArrayList prodList = null;
            ArrayList metricIDsList = null;
            ArrayList metricValuesList = null;
            if(session.ContainsKey("ComponentList"))
            {
                componentList = (ArrayList)session["ComponentList"];
            }
            if (session.ContainsKey("ProductList"))
            {
                prodList = (ArrayList)session["ProductList"];
            }
            if (session.ContainsKey("MetricIDList"))
            {
                metricIDsList = (ArrayList)session["MetricIDList"];
            }
            if (session.ContainsKey("MetricValueList"))
            {
                metricValuesList = (ArrayList)session["MetricValueList"];
            }
           
            
            string strCompIDList = string.Empty;
            string strProdIDList = string.Empty;
            string strMetricIDList = string.Empty;
            string strMetricValueList = string.Empty;

            if (componentList != null)
                strCompIDList = string.Join(",", (string[])componentList.ToArray(typeof(string)));
            if (prodList != null)
                strProdIDList = string.Join(",", (string[])prodList.ToArray(typeof(string)));
            if (metricIDsList != null)
                strMetricIDList = string.Join(",", (string[])metricIDsList.ToArray(typeof(string)));
            if (metricValuesList != null)
                strMetricValueList = string.Join(",", (string[])metricValuesList.ToArray(typeof(string)));





            SqlCommand sqlProdCommand = new SqlCommand();
            sqlProdCommand.CommandType = CommandType.StoredProcedure;
            sqlProdCommand.CommandText = "sp_InsertSerialAuditInfo";
            sqlProdCommand.Connection = sqlConn;
            sqlProdCommand.CommandTimeout = 120;
            //foreach (RadListBoxItem serial in NewKeyListBox.Items)
            //{
                sqlProdCommand.Parameters.AddWithValue("@serialNumber", newKeys);
                sqlProdCommand.Parameters.AddWithValue("@createdBy", strCreatedBy);
                sqlProdCommand.Parameters.AddWithValue("@CompIDList", strCompIDList);
                sqlProdCommand.Parameters.AddWithValue("@ProdIDList", strProdIDList);
                sqlProdCommand.Parameters.AddWithValue("@MetricIDList", strMetricIDList);
                sqlProdCommand.Parameters.AddWithValue("@MetricValueList", strMetricValueList);
                sqlConn.Open();
                sqlProdCommand.ExecuteNonQuery();
                sqlProdCommand.Parameters.Clear();
                sqlConn.Close();

            //////}

            #endregion

            #region Update serial organization table

            int OrganizationID = Convert.ToInt32(strOrganizationID);
            SqlCommand commandSQL = new SqlCommand();
            commandSQL.CommandType = CommandType.StoredProcedure;
            commandSQL.CommandText = "sp_InsertSerialOrganizationInfo";
            commandSQL.Connection = sqlConn;
            //foreach (RadListBoxItem serial in NewKeyListBox.Items)
            //{
                commandSQL.Parameters.AddWithValue("@serialNumber", newKeys);
                commandSQL.Parameters.AddWithValue("@OrganizationID", OrganizationID);
                sqlConn.Open();
                commandSQL.ExecuteNonQuery();
                commandSQL.Parameters.Clear();
                sqlConn.Close();
            //}

            #endregion

           return newKeys;
        }

        private void SendEmail(RequestInfo details,string newKey)
        {
            string subject="New EE License Key - For Free Evaluation Created with 30 days duration";
            string body = "<br/><b>Product Name:</b>Training All Features<br/><b>License Key:</b><br/>" + newKey;
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential("support@stonebond.com", "Stonebond1"),
                EnableSsl = true
            };
            //using (MailMessage message = new MailMessage("support@stonebond.com", "schandwani@stonebond.com") //for dev only
            using (MailMessage message = new MailMessage("support@stonebond.com", details.Email)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                BodyEncoding = System.Text.Encoding.UTF8,

            }) 
            {
                client.Send(message);
            }
           //client.Send("support@stonebond.com", ConfigurationManager.AppSettings["SendEmailList"], Subject,CommentTextBox.Text+ Environment.NewLine +"License Keys are:"+ Environment.NewLine+ keys);
            
        }

        protected void QueryComponents(string strProductID, string strMajorComponentID, string strMajorComponentName, bool isNew)
        {
            XmlDocument xDoc = new XmlDocument();
            //xDoc = (XmlDocument)session["ProductXML"];
            if (session.ContainsKey("ProductXML"))
            {
                xDoc = (XmlDocument)session["ProductXML"];
            }
            else
            {
                xDoc = null;
            }

            ArrayList destList = new ArrayList();

            string strCompQuery = "SELECT a.Id_, b.componentName, a.ComponentID, a.MajorComponentID, a.minor_attr_id FROM sbt_product_components a, sbt_components b " +
                                        "WHERE a.ComponentID = b.id and a.ProductId =" + strProductID + " and a.MajorComponentID =" + strMajorComponentID;

            SqlConnection sqlCompConn = new SqlConnection(strConn);
            SqlCommand sqlCompcommand = new SqlCommand(strCompQuery, sqlCompConn);
            sqlCompcommand.CommandType = CommandType.Text;
            sqlCompConn.Open();
            SqlDataReader Compdr = sqlCompcommand.ExecuteReader();
            while (Compdr.Read())
            {
                string productCompID = Compdr["Id_"].ToString();
                string ComponentID = Compdr["ComponentID"].ToString();
                string componentName = Compdr["componentName"].ToString();
                string minorAttrID = Compdr["minor_attr_id"].ToString();
                string minorAttrName = (string)minorAttrHashTable[minorAttrID];

                if (!isNew)
                {
                    XmlNodeList majorCompNodeList = xDoc.SelectNodes("Product/MajorComponent");
                    foreach (XmlNode majorCompNode in majorCompNodeList)
                    {
                        if (majorCompNode.SelectSingleNode("MajorComponentID").InnerText == strMajorComponentID)
                        {

                            bool newComponent = true;
                            XmlNodeList compNodeList = majorCompNode.SelectNodes("Components/ComponentAttributeName");
                            foreach (XmlNode compNode in compNodeList)
                            {
                                if (compNode.InnerText == componentName + "/" + minorAttrName)
                                {
                                    newComponent = false;
                                    break;
                                }
                            }
                            if (newComponent)
                            {
                                XmlNode tempNode = xDoc.CreateElement("Components");
                                majorCompNode.AppendChild(tempNode);
                                XmlNode compnode = tempNode;
                                XmlElement x4 = xDoc.CreateElement("ComponentID");
                                x4.InnerText = ComponentID;
                                compnode.AppendChild(x4);
                                x4 = xDoc.CreateElement("ComponentAttributeID");
                                x4.InnerText = minorAttrID;
                                compnode.AppendChild(x4);
                                x4 = xDoc.CreateElement("ComponentAttributeName");
                                x4.InnerText = componentName + "/" + minorAttrName;
                                compnode.AppendChild(x4);
                                RadListBoxItem ri = new RadListBoxItem();
                                ri.Text = componentName + "/" + minorAttrName;
                                ri.DataItem = ComponentID + "/" + minorAttrID;
                                destList.Add(ri);
                            }
                        }
                    }
                }
                else
                {
                    if (xDoc == null)
                    {
                        xDoc = new XmlDocument();
                        XmlElement mainNode = xDoc.CreateElement("Product");
                        xDoc.AppendChild(mainNode);
                    }

                    XmlNode parentNode = xDoc.SelectSingleNode("Product");
                    XmlNode tempNode = xDoc.CreateElement("MajorComponent");
                    parentNode.AppendChild(tempNode);
                    XmlNode majorCompNode = tempNode;
                    tempNode = xDoc.CreateElement("MajorComponentID");
                    tempNode.InnerText = strMajorComponentID;
                    majorCompNode.AppendChild(tempNode);
                    tempNode = xDoc.CreateElement("MajorComponentName");
                    tempNode.InnerText = strMajorComponentName;
                    majorCompNode.AppendChild(tempNode);

                    tempNode = xDoc.CreateElement("Components");
                    majorCompNode.AppendChild(tempNode);
                    XmlNode compnode = tempNode;
                    XmlElement x4 = xDoc.CreateElement("ComponentID");
                    x4.InnerText = ComponentID;
                    compnode.AppendChild(x4);
                    x4 = xDoc.CreateElement("ComponentAttributeID");
                    x4.InnerText = minorAttrID;
                    compnode.AppendChild(x4);
                    x4 = xDoc.CreateElement("ComponentAttributeName");
                    x4.InnerText = componentName + "/" + minorAttrName;
                    compnode.AppendChild(x4);
                    RadListBoxItem ri = new RadListBoxItem();
                    ri.Text = componentName + "/" + minorAttrName;
                    ri.DataItem = ComponentID + "/" + minorAttrID;
                    destList.Add(ri);

                    isNew = false;
                }
            }

            sqlCompConn.Close();
            if (session.ContainsKey("ProductXML"))
            {
                session["ProductXML"] = xDoc;
            }
            else
            {
                session.Add("ProductXML",xDoc);
            }

           
            ArrayList oldList = null;
            if (session.ContainsKey(strMajorComponentID + "DestinationComponents"))
            {
                oldList = (ArrayList)session[strMajorComponentID + "DestinationComponents"];
            }
            if (oldList != null)
            {
                foreach (RadListBoxItem item in oldList)
                {
                    destList.Add(item);
                }
            }

            if (session.ContainsKey(strMajorComponentID + "DestinationComponents"))
            {
                session[strMajorComponentID + "DestinationComponents"] = destList;
            }
            else
            {
                session.Add(strMajorComponentID + "DestinationComponents", destList);
            }
            //Session[strMajorComponentID + "DestinationComponents"] = destList;
        }

        protected void QueryMetrics(string strMajorComponentID, string strProductID)
        {
            #region Query Metric Details and Build XML
            XmlDocument xDoc = new XmlDocument();
            
            if(session.ContainsKey("ProductXML"))
            {
                xDoc = (XmlDocument)session["ProductXML"];
            }
            else
            {
                xDoc = null;
            }
            XmlNode majorCompNode = null;
            XmlNodeList majorCompNodeList = xDoc.SelectNodes("Product/MajorComponent");
            foreach (XmlNode CompNode in majorCompNodeList)
            {
                if (CompNode.SelectSingleNode("MajorComponentID").InnerText == strMajorComponentID)
                {
                    majorCompNode = CompNode;
                    break;
                }
            }
            string strMetricQuery = "SELECT a.MajorComponentID, b.attr_name, b.IsMandatory, b.DataType, b.id, b.ParentID, a.value FROM SBT_PRODUCT_ATTRIBUTES a, sbt_attr_master b WHERE " +
                                        "MajorComponentID = " + strMajorComponentID + " and product_id = " + strProductID + " and a.MetricID = b.id and a.value != '-1' ";

            DataTable MetricTable = new DataTable();
            SqlDataAdapter metricAdapter = new SqlDataAdapter(strMetricQuery, strConn);
            metricAdapter.Fill(MetricTable);
            XmlDocument oldMetricDoc = null;
            if (session.ContainsKey(strMajorComponentID + "Metrics"))
            {
                if (session[strMajorComponentID + "Metrics"] != null)
                {
                    oldMetricDoc = (XmlDocument)session[strMajorComponentID + "Metrics"];
                    if (oldMetricDoc.SelectSingleNode("Metrics/Metric/MetricValue").InnerText == string.Empty)
                        oldMetricDoc = null;
                }
            }
            
            BuildMetricXML(MetricTable, strMajorComponentID);

            XmlDocument metricDoc = (XmlDocument)session[strMajorComponentID + "Metrics"];
            if (oldMetricDoc != null)
            {
                metricDoc = CompareMetricXML(oldMetricDoc, metricDoc);
                if (session.ContainsKey(strMajorComponentID + "Metrics"))
                {
                    session[strMajorComponentID + "Metrics"] = metricDoc;
                }
                else
                {
                    session.Add(strMajorComponentID + "Metrics",metricDoc);
                }
                
                XmlNode removeNode = majorCompNode.SelectSingleNode("Metrics");
                if (removeNode != null)
                    majorCompNode.RemoveChild(removeNode);
            }

            XmlNode metricNode = metricDoc.SelectSingleNode("/Metrics");
            majorCompNode.AppendChild(xDoc.ImportNode(metricNode, true));
            //Session["ProductXML"] = xDoc;
            if (session.ContainsKey("ProductXML"))
            {
                session["ProductXML"] = xDoc;
            }
            else
            {
                session.Add("ProductXML", xDoc);
            }
            #endregion
        }

        private void BuildMetricXML(DataTable metricTable, string strMajorCompID)
        {
            XmlDocument xmldoc = new XmlDocument();
            XmlElement tempNode = xmldoc.CreateElement("Metrics");
            xmldoc.AppendChild(tempNode);
            XmlNode parentnode = xmldoc.ChildNodes[0];


            foreach (DataRow metricDataRow in metricTable.Rows)
            {
                tempNode = xmldoc.CreateElement("Metric");
                parentnode.AppendChild(tempNode);
                XmlNode mainNode = tempNode;


                XmlElement x4 = xmldoc.CreateElement("Selected");
                x4.InnerText = "True";
                mainNode.AppendChild(x4);

                x4 = xmldoc.CreateElement("IsMandatory");
                x4.InnerText = metricDataRow["IsMandatory"].ToString();
                mainNode.AppendChild(x4);

                x4 = xmldoc.CreateElement("MetricValue");
                x4.InnerText = metricDataRow["value"].ToString();
                mainNode.AppendChild(x4);

                x4 = xmldoc.CreateElement("MetricName");
                x4.InnerText = metricDataRow["attr_name"].ToString();
                mainNode.AppendChild(x4);

                x4 = xmldoc.CreateElement("MetricID");
                x4.InnerText = metricDataRow["id"].ToString();
                mainNode.AppendChild(x4);

                x4 = xmldoc.CreateElement("MetricDataType");
                x4.InnerText = metricDataRow["DataType"].ToString();
                mainNode.AppendChild(x4);

                x4 = xmldoc.CreateElement("MetricComplexValue");
                x4.InnerText = "";
                mainNode.AppendChild(x4);

                x4 = xmldoc.CreateElement("ParentID");
                x4.InnerText = metricDataRow["ParentID"].ToString();
                mainNode.AppendChild(x4);

                x4 = xmldoc.CreateElement("MetricComplexAttr");
                x4.InnerText = "";
                mainNode.AppendChild(x4);
            }

            if (session.ContainsKey(strMajorCompID + "Metrics"))
            {
                session[strMajorCompID + "Metrics"] = xmldoc;
            }
            else
            {
                session.Add(strMajorCompID + "Metrics", xmldoc);
            }
            //Session[strMajorCompID + "Metrics"] = null;
            //Session[strMajorCompID + "Metrics"] = xmldoc;

        }

        private XmlDocument CompareMetricXML(XmlDocument oldMetricDoc, XmlDocument newMetricDoc)
        {
            bool isNewMetric = true;
            XmlNode parentNode = oldMetricDoc.ChildNodes[0];
            XmlNodeList xlist = newMetricDoc.SelectNodes("Metrics/Metric");
            foreach (XmlNode xNode in xlist)
            {
                string metricID = xNode.SelectSingleNode("MetricID").InnerText;
                //Check if the metricID exists in oldMetricDoc
                XmlNodeList xoldlist = oldMetricDoc.SelectNodes("Metrics/Metric");
                foreach (XmlNode xOldNode in xoldlist)
                {
                    if (metricID == xOldNode.SelectSingleNode("MetricID").InnerText)
                    {
                        isNewMetric = false;
                        xOldNode.SelectSingleNode("Selected").InnerText = "True";
                        break;
                    }
                }
                if (isNewMetric)
                    parentNode.AppendChild(oldMetricDoc.ImportNode(xNode, true));

            }

            return oldMetricDoc;
        }
    }

    public class RequestInfo
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Company { get; set; }
        public string DriveID { get; set; }
    }

    public class RadListBoxItem
    {
        public string Text { get; set; }
        public string DataItem { get; set; }
    }

    public class ValidationException
    {
        public ValidationException()
        {
            Result = true;
        }
        public string NewKey { get; set; }
        public bool Result { get; set; }
        public string validateMessage { get; set; }
    }
}
