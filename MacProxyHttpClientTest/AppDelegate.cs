//
// AppDelegate.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using AppKit;
using Foundation;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using ObjCRuntime;

namespace MacProxyHttpClientTest1234
{
	[Register ("AppDelegate")]
	public class AppDelegate : NSApplicationDelegate
	{
		public AppDelegate ()
		{
		}

		public override void DidFinishLaunching (NSNotification notification)
		{
			// Insert code here to initialize your application
			try {
				Connect ().ContinueWith (t => {
					if (t.IsFaulted) {
						var ex = t.Exception.GetBaseException ();
						Console.WriteLine (ex);
					}
				});
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}

		public override void WillTerminate (NSNotification notification)
		{
			// Insert code here to tear down your application
		}

		async Task Connect ()
		{
			var config = NSUrlSessionConfiguration.DefaultSessionConfiguration;
			config.RequestCachePolicy = NSUrlRequestCachePolicy.ReloadIgnoringLocalCacheData;
			config.URLCache = null;
			/*config.TLSMinimumSupportedProtocol = Security.SslProtocol.Tls_1_2;
			var cfNetwork = Dlfcn.dlopen (Constants.CFNetworkLibrary, 0);
			var httpProxyHostKey = Dlfcn.GetStringConstant (cfNetwork, "kCFStreamPropertyHTTPProxyHost");
			var httpProxyPortKey = Dlfcn.GetStringConstant (cfNetwork, "kCFStreamPropertyHTTPProxyPort");
			var httpsProxyHostKey = Dlfcn.GetStringConstant (cfNetwork, "kCFStreamPropertyHTTPSProxyHost");
			var httpsProxyPortKey = Dlfcn.GetStringConstant (cfNetwork, "kCFStreamPropertyHTTPSProxyPort");
			var proxyHost = new NSString ("192.168.1.102");
			var proxyPort = new NSNumber (8888);
			// Do any additional setup after loading the view.
			var dictionary = new NSDictionary (
				"HTTPEnable", 1,
				"HTTPSEnable", 1,
				httpProxyHostKey, proxyHost,
				httpProxyPortKey, proxyPort,
				httpsProxyHostKey, proxyHost,
				httpsProxyPortKey, proxyPort);
			config.ConnectionProxyDictionary = dictionary;*/
			//config.ConnectionProxyDictionary = dictionary;
			//config.URLCache.RemoveAllCachedResponses ();

			var handler = new NSUrlSessionHandler (config);
			handler.DisableCaching = true;
			handler.Credentials = new TestCredential ();
			var client = new HttpClient (handler);
			//string url = "https://www.google.co.uk";
			string url = "https://api.nuget.org/v3/index.json";
			//string url = "http://cambridge.org";
			var response = await client.GetAsync (url);
			if (response.StatusCode == System.Net.HttpStatusCode.OK) {
				Console.WriteLine ("OK");
			} else {
				Console.Write (response.StatusCode);
			}
		}
	}

	class TestCredential : ICredentials
	{
		public NetworkCredential GetCredential (Uri uri, string authType)
		{
			return null;
		}
	}
}
