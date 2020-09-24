# Image API

The functionality of the api is fairly simple. We just want to download an image (from unsplash), and we want to download some text from the wikipedia API. Then we are going to paste the text over the image using the ```ImageEditor``` class. Lastly we are going to store this image in the blob storage, where it can be downloaded by a user with a SAS token.

### Complexity due to the async model

The problem with the above approach is that it can take quite a long time for both the image to be downloaded, and text to be inserted into the image. Because we want to give the user a smooth user experience we are going to use the following infrastructure:

