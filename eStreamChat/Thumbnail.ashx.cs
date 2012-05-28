using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Net;
using System.Web;

namespace eStreamChat
{
    public class Thumbnail : IHttpHandler
    {
        #region IHttpHandler Members

        public void ProcessRequest(HttpContext context)
        {
            string imageUrl = context.Request.Params["img"];
            Image image;
            if (imageUrl.Contains("UserFiles"))
            {
                //imageUrl = imageUrl.Substring(imageUrl.IndexOf("UserFiles"));
                image = Image.FromFile(context.Server.MapPath(imageUrl));
            }
            else
            {
                WebClient wc = new WebClient();
                image = Image.FromStream(wc.OpenRead(imageUrl));
            }
            int width = Convert.ToInt32(context.Request.Params["width"] ?? "400");
            int height = Convert.ToInt32(context.Request.Params["height"] ?? "200");
            Image thumbnail = ResizeImage(image, width, height);
            if (image != thumbnail)
                image.Dispose();

            context.Response.Clear();
            context.Response.Cache.SetExpires(DateTime.Now.AddSeconds(600));
            context.Response.Cache.SetCacheability(HttpCacheability.Public);
            context.Response.Cache.SetValidUntilExpires(false);
            context.Response.Cache.VaryByParams["img"] = true;
            context.Response.Cache.SetAllowResponseInBrowserHistory(true);
            context.Response.ContentType = "image/jpeg";
            thumbnail.Save(context.Response.OutputStream, ImageFormat.Jpeg);
            thumbnail.Dispose();
        }

        public bool IsReusable
        {
            get { return true; }
        }

        #endregion

        public static Image ResizeImage(Image image, int MaxWidth, int MaxHeight)
        {
            int newWidth, newHeight;

            if ((double) MaxWidth/(double) image.Width
                < (double) MaxHeight/(double) image.Height
                && image.Width > MaxWidth)
            {
                newWidth = MaxWidth;
                newHeight = Convert.ToInt32(Convert.ToDouble(image.Height)
                                            *((double) MaxWidth/Convert.ToDouble(image.Width)));
            }
            else if ((double) MaxHeight/ (double)image.Height
                     <= (double) MaxWidth/(double) image.Width
                     && image.Height > MaxHeight)
            {
                newHeight = MaxHeight;
                newWidth = Convert.ToInt32(Convert.ToDouble(image.Width)
                                           *((double) MaxHeight/Convert.ToDouble(image.Height)));
            }
            else
            {
                return image;
            }

            Bitmap bmp = new Bitmap(newWidth, newHeight);
            Graphics g = Graphics.FromImage(bmp);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(image, 0, 0, newWidth, newHeight);

            return bmp;
        }
    }
}