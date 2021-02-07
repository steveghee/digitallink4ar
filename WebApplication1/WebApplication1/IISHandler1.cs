using System;
using System.Web;
using System.Collections.Specialized;

namespace WebApplication1
{
    public class IISHandler1 : IHttpHandler
    {
        /// <summary>
        /// You will need to configure this handler in the Web.config file of your 
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: https://go.microsoft.com/?linkid=8101007
        /// </summary>
        #region IHttpHandler Members

        public bool IsReusable
        {
            // Return false in case your Managed Handler cannot be reused for another request.
            // Usually this would be false in case you have some state information preserved per request.
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            string path = context.Request.Path;
            string[] tokens = path.Split( new char[] {'/' });

            string respstr = "";
            bool isGTIN = false;
            bool isSSCC = false;
            string GTINID = "";
            string SNUMBR = "";
            string BATCH  = "";
            string SSCCID = "";
            string COUNT  = "";
            string WEIGHT = "";
            // we are looking for something of this form - SGTIN    = /01/00095791000015/21/0000123 
            // or                                     GTIN+batch    = /01/00095791000015/10/ABC123
            // or                                     GTIN+batch+SN = /01/00095791000015/10/ABC123/21/0000123  :: batch before SN
            // or                                     SSCC          = /00/00095791000015[?02=GTIN&3101=w&37=n]

            for (int i = 0; i < tokens.Length; i++)
            {
                // 00 or 01 MUST come first
                if (!isGTIN && !isSSCC && tokens[i] == "00" && tokens.Length > (i+1))
                {
                    SSCCID = tokens[i + 1];  // ideally we would check this is 18 chars as per SSCC defintion
                    isSSCC = SSCCID.Length == 18 ? true : false;
                }
                if (!isGTIN && !isSSCC && tokens[i] == "01" && tokens.Length > (i + 1))
                {
                    GTINID = tokens[i + 1];  // ideally we would check this is 8/12/13/14 chars as for GTIN spec
                    isGTIN = true;
                }

                // followed by optional data
                if (isGTIN && tokens[i] == "21" && tokens.Length > (i + 1))
                {
                    SNUMBR = tokens[i + 1]; // sreialised gtin
                }
                if (isGTIN && tokens[i] == "10" && tokens.Length > (i + 1))
                {
                    BATCH = tokens[i + 1];  // batch/lot
                }
                respstr = respstr + tokens[i];
            }

            // and process any optional keys (SSCC only)
            if (isSSCC)
            {
                NameValueCollection coll = context.Request.QueryString;
                foreach (String key in coll.AllKeys)
                {
                    if (String.Compare(key, "02") == 0)
                        GTINID = coll.GetValues(key)[0];
                    if (String.Compare(key, "37") == 0)
                        COUNT = coll.GetValues(key)[0];
                    if (String.Compare(key, "3101") == 0)
                        WEIGHT = coll.GetValues(key)[0];
                }

            }

            //write your handler implementation here.
            HttpResponse response = context.Response;
            if (!isGTIN && !isSSCC)
            {
                response.Write("<html><body><h1>path is " + path + "</h1>We were given" + tokens.Length + " tokens");
                response.Write("tokens " + respstr);
                response.Write("</body></html>");
            }
            else
            {
                string options = isGTIN ? string.Format("%26GTIN%3D{0}%26SERIAL%3D{1}%26BATCH%3D{2}", GTINID, SNUMBR, BATCH)
                                        : string.Format("%26SSCC%3D{0}%26GTIN%3D{1}%26COUNT%3D{2}%26WEIGHT%3D{3}",SSCCID, GTINID, COUNT, WEIGHT);

                // this we would lookup based on the primary code (SCCID or GTINID)
                string experience = "https://view.vuforia.com/command/view-experience?url=https%3A%2F%2Fxuqztwnu.pp.vuforia.io%2FExperienceService%2Fcontent%2Fprojects%2Fnordlock%2Findex.html%3FexpId%3D1";
                
                // and then we build the response
                respstr = string.Format("{0}{1}", experience, options);
#if DEBUG
                response.Write("<html><body><h1>path is " + path + "</h1>We were given" + tokens.Length + " tokens");
                response.Write("redirect to " + respstr);
                response.Write("</body></html>");
#else
                // and redirect to the experience server
                response.Redirect(respstr, true);
#endif
            }

        }

#endregion
    }
}
