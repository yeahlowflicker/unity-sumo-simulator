using UnityEngine;
using System.Collections;
using System.IO;
using SimpleFileBrowser;

public class SimFileBrowser : MonoBehaviour
{
    public static SimFileBrowser instance;
    public string selectedFilePath = "";

	// Warning: paths returned by FileBrowser dialogs do not contain a trailing '\' character
	// Warning: FileBrowser can only show 1 dialog at a time

    void Awake() {
        instance = this;
    }

	void Start()
	{
		// Set filters (optional)
		// It is sufficient to set the filters just once (instead of each time before showing the file browser dialog), 
		// if all the dialogs will be using the same filters
		FileBrowser.SetFilters( true, new FileBrowser.Filter( "XML", ".xml" ) );

		// Set default filter that is selected when the dialog is shown (optional)
		// Returns true if the default filter is set successfully
		// In this case, set Images filter as the default filter
		FileBrowser.SetDefaultFilter( ".xml" );

		// Set excluded file extensions (optional) (by default, .lnk and .tmp extensions are excluded)
		// Note that when you use this function, .lnk and .tmp extensions will no longer be
		// excluded unless you explicitly add them as parameters to the function
		FileBrowser.SetExcludedExtensions( ".lnk", ".tmp", ".zip", ".rar", ".exe" );

		// Add a new quick link to the browser (optional) (returns true if quick link is added successfully)
		// It is sufficient to add a quick link just once
		// Name: Users
		// Path: C:\Users
		// Icon: default (folder icon)
		FileBrowser.AddQuickLink( "Users", "C:\\Users", null );
	}


    public void BrowseFile() {
		StartCoroutine( ShowLoadDialogCoroutine() );
    }


	IEnumerator ShowLoadDialogCoroutine()
	{
		// Show a load file dialog and wait for a response from user
		// Load file/folder: file, Allow multiple selection: true
		// Initial path: default (Documents), Initial filename: empty
		// Title: "Load File", Submit button text: "Load"
		yield return FileBrowser.WaitForLoadDialog( FileBrowser.PickMode.Files, true, null, null, "Select Files", "Load" );

		// Dialog is closed
		// Print whether the user has selected some files or cancelled the operation (FileBrowser.Success)
		Debug.Log( FileBrowser.Success );

		if( FileBrowser.Success )
            Simulator.instance.OnXMLInputFilePathSelected(FileBrowser.Result[0]);
	}
	

    public void ShowSaveDialog() {
		StartCoroutine( ShowSaveDialogCoroutine() );
    }


    IEnumerator ShowSaveDialogCoroutine()
	{
		// Show a load file dialog and wait for a response from user
		// Load file/folder: file, Allow multiple selection: true
		// Initial path: default (Documents), Initial filename: empty
		// Title: "Load File", Submit button text: "Load"
		yield return FileBrowser.WaitForSaveDialog( FileBrowser.PickMode.Files, true, null, null, "Save As", "Save" );

		// Dialog is closed
		// Print whether the user has selected some files or cancelled the operation (FileBrowser.Success)
		Debug.Log( FileBrowser.Success );

		if( FileBrowser.Success ) {
            Simulator.instance.OnXMLOutputPathSelected(FileBrowser.Result[0]);
            Debug.Log(FileBrowser.Result[0]);
        }
	}


	void OnFilesSelected( string[] filePaths )
	{
		// Print paths of the selected files
		for( int i = 0; i < filePaths.Length; i++ )
			Debug.Log( filePaths[i] );

		// Get the file path of the first selected file
		string filePath = filePaths[0];
	}



	/*
	*	Initialize a XML file to ensure it exists.
	*/
	public static void CreateXMLFileIfNotExists(string filePath) {

		// Create the file
		using (FileStream fs = File.Create(filePath))
		{
			// Optionally, you can write the XML structure to the file
			using (StreamWriter writer = new StreamWriter(fs))
			{
				// Write a valid XML structure with an empty body
				writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
				writer.WriteLine("<root></root>");
			}
		}
		Debug.Log("XML file created successfully at: " + filePath);
	}
}