using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nimble
{
	public class StaticFileSettings
	{
		public static StaticFileSettings defaultSettings { get; private set; }

		public List<string> indexFiles { get; private set; } = new List<string>() {
			"index.html",
			"index.htm",
			"default.html",
			"default.htm"
		};

		public static IDictionary<string, string> contentTypeMappings { get; private set; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
			{".asf", "video/x-ms-asf"},
			{".asx", "video/x-ms-asf"},
			{".avi", "video/x-msvideo"},
			{".bin", "application/octet-stream"},
			{".cco", "application/x-cocoa"},
			{".crt", "application/x-x509-ca-cert"},
			{".css", "text/css"},
			{".deb", "application/octet-stream"},
			{".der", "application/x-x509-ca-cert"},
			{".dll", "application/octet-stream"},
			{".dmg", "application/octet-stream"},
			{".ear", "application/java-archive"},
			{".eot", "application/octet-stream"},
			{".exe", "application/octet-stream"},
			{".flv", "video/x-flv"},
			{".gif", "image/gif"},
			{".hqx", "application/mac-binhex40"},
			{".htc", "text/x-component"},
			{".htm", "text/html"},
			{".html", "text/html"},
			{".ico", "image/x-icon"},
			{".img", "application/octet-stream"},
			{".iso", "application/octet-stream"},
			{".jar", "application/java-archive"},
			{".jardiff", "application/x-java-archive-diff"},
			{".jng", "image/x-jng"},
			{".jnlp", "application/x-java-jnlp-file"},
			{".jpeg", "image/jpeg"},
			{".jpg", "image/jpeg"},
			{".js", "application/x-javascript"},
			{".mml", "text/mathml"},
			{".mng", "video/x-mng"},
			{".mov", "video/quicktime"},
			{".mp3", "audio/mpeg"},
			{".mpeg", "video/mpeg"},
			{".mpg", "video/mpeg"},
			{".msi", "application/octet-stream"},
			{".msm", "application/octet-stream"},
			{".msp", "application/octet-stream"},
			{".pdb", "application/x-pilot"},
			{".pdf", "application/pdf"},
			{".pem", "application/x-x509-ca-cert"},
			{".pl", "application/x-perl"},
			{".pm", "application/x-perl"},
			{".png", "image/png"},
			{".prc", "application/x-pilot"},
			{".ra", "audio/x-realaudio"},
			{".rar", "application/x-rar-compressed"},
			{".rpm", "application/x-redhat-package-manager"},
			{".rss", "text/xml"},
			{".run", "application/x-makeself"},
			{".sea", "application/x-sea"},
			{".shtml", "text/html"},
			{".sit", "application/x-stuffit"},
			{".swf", "application/x-shockwave-flash"},
			{".tcl", "application/x-tcl"},
			{".tk", "application/x-tcl"},
			{".txt", "text/plain"},
			{".war", "application/java-archive"},
			{".wbmp", "image/vnd.wap.wbmp"},
			{".wmv", "video/x-ms-wmv"},
			{".xml", "text/xml"},
			{".xpi", "application/x-xpinstall"},
			{".zip", "application/zip"},
			{".json", "application/json"},
		};

		static StaticFileSettings()
		{
			defaultSettings = new StaticFileSettings();
		}

		public string GetContentTypeForExtension(string extension)
		{
			if (!extension.StartsWith("."))
			{
				extension = "."+extension;
			}

			string mime;
			if (contentTypeMappings.TryGetValue(extension, out mime))
			{
				return mime;
			}
			return "application/octet-stream";
		}
	}
}
