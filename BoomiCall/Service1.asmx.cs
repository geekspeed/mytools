using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Services;
using System.Net;
using System.Drawing;
using System.IO;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Drawing.Imaging;
using System.DirectoryServices;
using System.Configuration;

namespace BoomiCall
{
    /// <summary>
    /// Summary description for Service1
    /// </summary>
    [WebService(Namespace = "http://boomi.arubanetworks.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    public class Service1 : System.Web.Services.WebService
    { 

        [WebMethod]
        public void downloadImage(string empNumber)
        {

            DirectoryEntry entry = new DirectoryEntry(ConfigurationSettings.AppSettings["AD_SERVER_URL"].ToString(),
                               ConfigurationSettings.AppSettings["AD_SERVER_USERNAME"].ToString(), ConfigurationSettings.AppSettings["AD_SERVER_PASSWORD"].ToString());


            DirectorySearcher Dsearch = new DirectorySearcher(entry);
            Dsearch.Filter = String.Format("(&(objectClass=User)(employeeNumber="+empNumber+"))");
            SearchResultCollection collection = Dsearch.FindAll();
            foreach (SearchResult sResultSet in collection)
            {
                using (DirectoryEntry user = new DirectoryEntry(sResultSet.Path))
                {
                    if (user.Properties["thumbnailPhoto"].Value != null)
                    {
                        byte[] data = user.Properties["thumbnailPhoto"].Value as byte[];

                        if (data != null)
                        {
                            File.WriteAllBytes(ConfigurationSettings.AppSettings["PHOTOS_DOWNLOAD_PATH"].ToString() + empNumber + ConfigurationSettings.AppSettings["PHOTO_EXTENSION"].ToString(), data);
                            
                           
                        }
                    }
                }
            }

            //Bitmap bmpReturn = null;

            //byte[] bytes = Convert.FromBase64String(imgtext.Split('*')[1]);
            //MemoryStream memoryStream = new MemoryStream(bytes);

            //memoryStream.Position = 0;

            //bmpReturn = (Bitmap)Bitmap.FromStream(memoryStream);

            //memoryStream.Close();
            //memoryStream = null;
            //bytes = null;

            ////return bmpReturn;

            //Bitmap bitmap = new Bitmap(96, 96);
            //Graphics graphics = Graphics.FromImage(bitmap);
            ////  bmpReturn = new Bitmap(bmpReturn, new Size(96, 96));
            //bitmap.Save(fullOutputPath, ImageFormat.Jpeg);






            //Image image;
            //using (MemoryStream ms = new MemoryStream(bytes))
            //{
            //    image = Image.FromStream(ms);
            //}

            //image.Save(fullOutputPath, System.Drawing.Imaging.ImageFormat.Jpeg);


            //}
        }
        

        [WebMethod]
        public wresponse UploadImage(string imgtext)
        {

            wresponse outObj =  new wresponse();

            if (!string.IsNullOrEmpty(imgtext))
            {
                string[] data = imgtext.Split('*');
                if (data.Length == 2)
                {
                    long empNumber=-1;
                    if (long.TryParse(data[0].ToString(), out empNumber))
                    {
                        if (!string.IsNullOrEmpty(data[1]))
                        {
                            byte[] jpegImage = ImageFromBase64String(data[1]);
                            if (jpegImage != null && jpegImage.Length > 0)
                            {

                                outObj = SetADPhoto(jpegImage, empNumber.ToString());
                            }
                            else
                            {
                                outObj.status = false;
                                outObj.errmessage = "Picture conversion failed for EmployeeNumber:" + data[0].ToString();
                            }
                        }
                        else
                        {
                            outObj.status = false;
                            outObj.errmessage = "No picture data Number found for EmployeeNumber:" + data[0].ToString();
                        }
                            
                     

                    }
                    else
                    {

                        outObj.status = false;
                        outObj.errmessage = "Employee Number is not an number:"+data[0].ToString();
                    }
                }
                else
                {
                    
                    outObj.status = false;
                    outObj.errmessage = "Input string in incorrect format, missing seperator *";
                
                }
                
                
            }
            else
            {
                outObj.status = false;
                outObj.errmessage = "Input string is empty";
            }

            return outObj;

           
        }
        
        public string ImageToBase64String(string x)
        {
            Image image = Image.FromFile(@"C:\Users\vivek\Downloads\untitled.png");
            using (MemoryStream stream = new MemoryStream())
            {
                image.Save(stream, image.RawFormat);
                File.WriteAllText(@"C:\Users\vivek\Downloads\" + "asample" + ".txt", Convert.ToBase64String(stream.ToArray()));
                return "1";
            }
        }

        /// <summary>
        /// Creates a new image from the given base64 encoded string.
        /// </summary>
        /// <param name="base64String">The encoded image data as a string.</param>
        byte[] ImageFromBase64String(string base64String)
        {
            byte[] imArray;
            using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(base64String)))
            using (Image sourceImage = Image.FromStream(stream))
            {
                //string fullOutputPath = @"C:\Users\vivek\Downloads\" + "asample" + ".jpg";
                MemoryStream ms =  new MemoryStream();
                new Bitmap(sourceImage, new Size(96, 96)).Save(ms, ImageFormat.Jpeg);

                imArray= ms.ToArray();
                sourceImage.Dispose();
                ms.Close();
                ms.Dispose();
                stream.Close();
                stream.Dispose();
                

            }
            


            return imArray;
        }


        wresponse SetADPhoto(byte[] ba, string employeeNumber)
        {
            wresponse retVal = new wresponse();
            try
            {

                var de = GetObjectDistinguishedName(employeeNumber);

                de.Username = ConfigurationManager.AppSettings["AD_SERVER_USERNAME"];
                de.Password = ConfigurationManager.AppSettings["AD_SERVER_PASSWORD"];
                de.Properties["thumbnailPhoto"].Clear();
                de.Properties["thumbnailPhoto"].Add(ba);//Insert(0, ba);
                de.CommitChanges();
                retVal.status = true;
            }
            catch (Exception ex)
            {
                retVal.status = false;
                retVal.errmessage = "AD photo update failed for EmployeeNumber:" + employeeNumber.ToString() + "...." + ex.ToString();
            }

            return retVal;
        }

        DirectoryEntry GetObjectDistinguishedName(string employeeNumber)
        {
            string distinguishedName = string.Empty;
            string connectionPrefix = ConfigurationManager.AppSettings["AD_SERVER_URL"];
            DirectoryEntry entry = new DirectoryEntry(connectionPrefix);
            DirectorySearcher mySearcher = new DirectorySearcher(entry);
            mySearcher.Filter = "(&(objectClass=user)((employeeNumber=" + employeeNumber + ")))";

            SearchResult result = mySearcher.FindOne();

            if (result == null)
            {
                throw new NullReferenceException("unable to locate the AD record for the EmployeeNumber " + employeeNumber);
            }
            DirectoryEntry directoryObject = result.GetDirectoryEntry();
            if (directoryObject.Guid == null)
            {
                throw new NullReferenceException("unable to locate the AD Guid for the EmployeeNumber " + employeeNumber);
            }


            entry.Close();
            entry.Dispose();
            mySearcher.Dispose();
            return directoryObject;
        }
    }
    public class wresponse
    {
        public bool status = false;
        public string errmessage = string.Empty;
        public wresponse()
        { 
        
        }
   
    }
}