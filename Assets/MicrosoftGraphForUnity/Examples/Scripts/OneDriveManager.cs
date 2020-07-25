using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;
using UnityEngine;
using UnityEngine.UI;

namespace MicrosoftGraphForUnity.Examples
{
    /// <summary>
    /// This example shows how to query OneDrive from a Microsoft Graph account and present the results in a list.
    /// </summary>
    public class OneDriveManager : MonoBehaviour
    {
        [Header("Misc")]
        [SerializeField]
        private MicrosoftGraphManager graphManager;
        [SerializeField]
        private UIDriveItemElement driveItemPrefab;
        [SerializeField]
        private Sprite placeHolderThumbnail;

        [Header("UI")]
        [SerializeField]
        private InputField inputField;
        [SerializeField]
        private Button searchButton;
        [SerializeField]
        private Text searchButtonText;
        [SerializeField]
        private Transform contentRoot;

        private List<UIDriveItemElement> foundItems;
        private bool isSearching;
        private bool cancelSearch;

        private void Start()
        {
            searchButton.onClick.AddListener(HandleOnSearchButtonClick);
        }

        private async void SignOut()
        {
            await graphManager.AuthenticationService.SignOutAsync();
        }

        private async void HandleOnSearchButtonClick()
        {
            if (isSearching)
            {
                cancelSearch = true;
                return;
            }
            
            if (string.IsNullOrWhiteSpace(inputField.text))
            {
                return;
            }

            isSearching = true;
            searchButtonText.text = "Cancel";
            var searchResult = await SearchDrive(graphManager.Client.Drive, inputField.text);
            if (!searchResult.Any())
            {
                isSearching = false;
                cancelSearch = false;
                searchButtonText.text = "Search";
                return;
            }

            if (foundItems != null && foundItems.Any())
            {
                foreach (var searchItem in foundItems)
                {
                    Destroy(searchItem.gameObject);
                }
            }
            
            foundItems = new List<UIDriveItemElement>();
            foreach (var driveItem in searchResult)
            {
                if (cancelSearch)
                {
                    break;
                }
                var item = Instantiate(driveItemPrefab, contentRoot);
                item.transform.SetAsLastSibling();
                item.text.text = driveItem.Name;
                var sprite = await DownloadDriveItemThumbnail(graphManager.Client.Drive, driveItem.Id);
                item.image.sprite = sprite != null ? sprite : placeHolderThumbnail;
                foundItems.Add(item);
            }
            
            isSearching = false;
            cancelSearch = false;
            searchButtonText.text = "Search";
        }

        /// <summary>
        /// Search the drive by the given <see href="https://docs.microsoft.com/en-us/graph/query-parameters">OData Query</see> string.
        /// </summary>
        /// <param name="drive">Target drive to search at.</param>
        /// <param name="query">Search text query</param>
        /// <returns>Found DriveItems</returns>
        private async Task<List<DriveItem>> SearchDrive(IDriveRequestBuilder drive, string query)
        {
            var search = await drive.Search(query).Request().GetAsync();
            return search.ToList();
        }

        /// <summary>
        /// Download the primary mid sized thumbnail as a Sprite.
        /// </summary>
        /// <param name="drive">Target drive.</param>
        /// <param name="itemId">Source item id.</param>
        /// <returns>Thumbnail as Sprite or null if the item has no thumbnail.</returns>
        private async Task<Sprite> DownloadDriveItemThumbnail(IDriveRequestBuilder drive, string itemId)
        {
            var thumbnails = await drive.Items[itemId].Thumbnails.Request().GetAsync();
            if (!thumbnails.Any())
            {
                return null;
            }
            
            var thumbnail = thumbnails.First();
            var content = await drive.Items[itemId].Thumbnails[thumbnail.Id]["medium"].Content.Request().GetAsync();
            using (var reader = new MemoryStream())
            {
                await content.CopyToAsync(reader);
                var data  = reader.ToArray();
                var texture = new Texture2D(0, 0);
                texture.LoadImage(data);
                return Sprite.Create(
                    texture, 
                    new Rect(0, 0, texture.width, texture.height), 
                    new Vector2(0.5f, 0.5f));
            }
        }
    }
}
