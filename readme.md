# Image API
The functionality of the api is fairly simple. We just want to download an image (from unsplash), and we want to download some text from the wikipedia API. Then we are going to paste the text over the image using the ```ImageEditor``` class. Lastly we are going to store this image in the blob storage, where it can be downloaded by a user with a SAS token.

### Complexity due to the async model
The problem with the above approach is that it can take quite a long time for both the image to be downloaded, and text to be inserted into the image. Because we want to give the user a smooth user experience we are going to use the following infrastructure:

![Infrastructure](https://github.com/Mathuiss/ImageApi/blob/master/extra/Infrastructure.png)

## How it works
The caller makes a request to the ```/images``` endpoint with the ```?query={query}``` parameters. You can query the api with whatever you want, and the system will create a file handle for you. The system will also create a 0 bytes temp file in the blob storage with this file handle. It will then continue to generate a url with a sas token (valid for 1 hour) so that you, once the image has been generated, can download it directly from the server.

In the mean time the query is placed in the queue, and a QueueHandler is trying to download an image from unsplash and some text from wikipedia. The image will be stored in the blob storage, with the previously made file handle, thus overwriting the 0 bytes temp file. So even if the final image has not been generated, and you try to download your image, you will see the image that has been downloaded from unslash.

Once both the image and the text are done downloading, they are placed in the ```imagequeue```. This queue will take the file handle and the text and it will place the downloaded text over the downloaded file and overwrite the file in the blob storage. At this point, the user already has access to the file through the url from the ```Entrypoint HTTP trigger```. This means that the system has no way to communicate to the user. In case anything goes wrong, a 404 image will be downloaded and an error message will be pasted over this image. This means that if the user downloads the final image and anything has gone wrong, the user will see that an error has occured.