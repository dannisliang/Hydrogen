﻿#region Copyright Notice & License Information
//
// CheckForUpdate.cs
//
// Author:
//       Matthew Davey <matthew.davey@dotbunny.com>
//
// Copyright (c) 2014 dotBunny Inc. (http://www.dotbunny.com)
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
#endregion

using System;
using System.IO;
using System.Net;
using UnityEditor;

public static class CheckForUpdate
{
		[MenuItem ("Help/Check for Hydrogen Update", false, 200)]
		public static void CheckUpdate ()
		{
			try 
			{
				EditorUtility.DisplayCancelableProgressBar("Checking for Hydrogen Updates", "Contacting GitHub ...", 0.1f);
				
				WebClient client = new WebClient();
				string changelogHTML = client.DownloadString(HydrogenUtility.ChangelogURI);

				System.Version latestVersion = new Version(
					changelogHTML.Substring(0, changelogHTML.IndexOf("\n")).Replace("Version ", ""));

				EditorUtility.DisplayCancelableProgressBar("Checking for Hydrogen Updates", "Processing information ...", 0.2f);
	
				if (latestVersion.CompareTo(HydrogenUtility.Version) > 0 ) 
				{ 
					EditorUtility.ClearProgressBar();
					int decision = EditorUtility.DisplayDialogComplex("Hydrogen Update Available", "Do you wish to update your version of the Hydrogen Framework? \n\nThis will OVERWRITE any chances you've made to the framework.", "No", "Yes", string.Empty);
					
					if ( decision == 1 ) {
					EditorUtility.DisplayCancelableProgressBar("Updating Hydrogen", "Downloading latest (" + latestVersion.ToString() + ") ...", 0.3f);
						byte[] package = client.DownloadData(HydrogenUtility.PackageURI);
				
						string tempFile = Path.GetTempFileName();
						string tempFolder = Path.GetTempPath() + "Hydrogen_" + latestVersion.ToString();
						
						client.DownloadFile(HydrogenUtility.PackageURI, tempFile);


						EditorUtility.DisplayCancelableProgressBar("Updating Hydrogen", "Extracting ...", 0.6f);
						
						// Create Temp Extraction Folder
						if ( Directory.Exists(tempFolder) ) 
						{
							Directory.Delete(tempFolder, true);
						}
						Directory.CreateDirectory(tempFolder);

						HydrogenUtility.ExtractZipFile(tempFile, "", tempFolder);

						EditorUtility.DisplayCancelableProgressBar("Updating Hydrogen", "Moving Into Place ...", 0.8f);

						if ( Directory.Exists(HydrogenUtility.GetHydrogenPath()) )
						{
							Directory.Delete (HydrogenUtility.GetHydrogenPath(), true);
						}
					    
						Directory.Move(tempFolder + Path.DirectorySeparatorChar + "Hydrogen-master", HydrogenUtility.GetHydrogenPath());
					}
				}

				// Save time of last checking so we only do it every so often
				EditorPrefs.SetString("HydrogenFramework_LastChecked", DateTime.Now.ToString());
			}
			catch(System.Exception e)
			{
				UnityEngine.Debug.LogWarning("Unable to update Hydogen. " + "\n" + e.Message + "\n" + e.Source + "\n" + e.StackTrace);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		
}