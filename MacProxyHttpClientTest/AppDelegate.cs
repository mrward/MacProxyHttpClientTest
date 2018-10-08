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

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AppKit;
using CoreFoundation;
using Foundation;

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
			var request = new HttpRequestMessage (HttpMethod.Get, url);
			AddHeaders (request);
			var response = await client.SendAsync (request);
			if (response.StatusCode == System.Net.HttpStatusCode.OK) {
				Console.WriteLine ("OK");
			} else {
				Console.Write (response.StatusCode);
			}
		}

		void AddHeaders (HttpRequestMessage request)
		{
			ProxyInfo proxy = GetProxy (request.RequestUri);
			if (proxy == null || proxy.ProxyType != CFProxyType.HTTPS)
				return;

			NSUrlCredential credential = GetProxyCredential (proxy);
			if (credential == null)
				return;

			string auth = GetBasicAuthHeaderValue (credential);
			if (string.IsNullOrEmpty (auth))
				return;

			request.Headers.ProxyAuthorization = new AuthenticationHeaderValue ("Basic", auth);
		}

		static NSUrlCredential GetProxyCredential (ProxyInfo proxy)
		{
			foreach (NSObject key in NSUrlCredentialStorage.SharedCredentialStorage.AllCredentials.Keys) {
				var protectionSpace = key as NSUrlProtectionSpace;
				if (!IsProtectionSpaceForProject (protectionSpace, proxy))
					continue;

				// Only basic auth and HTTPS is supported.
				if (proxy.ProxyType != CFProxyType.HTTPS || protectionSpace.AuthenticationMethod != NSUrlProtectionSpace.AuthenticationMethodHTTPBasic)
					continue;

				var dictionary = NSUrlCredentialStorage.SharedCredentialStorage.AllCredentials [key] as NSDictionary;
				if (dictionary == null)
					continue;

				foreach (var value in dictionary) {
					var credential = value.Value as NSUrlCredential;
					if (credential != null)
						return credential;
				}
			}
			return null;
		}

		static bool IsProtectionSpaceForProject (NSUrlProtectionSpace protectionSpace, ProxyInfo proxy)
		{
			return protectionSpace != null &&
				protectionSpace.IsProxy &&
				protectionSpace.Port == proxy.Port &&
				StringComparer.OrdinalIgnoreCase.Equals (protectionSpace.ProxyType, proxy.ProxyType.ToString ()) &&
				StringComparer.OrdinalIgnoreCase.Equals (protectionSpace.Host, proxy.HostName);
		}

		static ProxyInfo GetProxy (Uri requestUri)
		{
			IWebProxy systemProxy = WebRequest.GetSystemWebProxy ();
			Uri proxyUri = systemProxy.GetProxy (requestUri);
			var proxyAddress = new Uri (proxyUri.AbsoluteUri);

			if (string.Equals (proxyAddress.AbsoluteUri, requestUri.AbsoluteUri))
				return null;
			if (systemProxy.IsBypassed (requestUri))
				return null;

			var proxyType = GetProxyType (requestUri);
			return new ProxyInfo {
				Port = proxyAddress.Port,
				ProxyType = GetProxyType (requestUri),
				HostName = proxyAddress.Host
			};
		}

		static CFProxyType GetProxyType (Uri requestUri)
		{
			switch (requestUri.Scheme.ToLower ()) {
				case "https":
				return CFProxyType.HTTPS;

				case "http":
				return CFProxyType.HTTP;

				default:
				return CFProxyType.None;
			}
		}

		string GetBasicAuthHeaderValue (NSUrlCredential credential)
		{
			if (string.IsNullOrEmpty (credential.User))
				return null;

			string password = credential.Password ?? string.Empty;
			byte[] bytes = GetBytes (credential.User + ":" + password);

			return Convert.ToBase64String (bytes);
		}

		/// <summary>
		/// From Mono's BasicClient
		/// </summary>
		static byte[] GetBytes (string str)
		{
			int i = str.Length;
			byte[] result = new byte[i];
			for (--i; i >= 0; i--)
				result[i] = (byte)str[i];

			return result;
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
