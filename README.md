# Spautofy

#### Note: I need to remove a couple nuget packages that aren't used still

Spautofy is a simple Desktop player/wrapper around the Spotify web player using the Spotify web API.  You can drag songs, albums, and playlists in to the queue, and full size art will be displayed.

## To get it working you will need to:
 - Sign up for a Spotify developer account and get an API key
 - Add the API key in to a SpotifyAPI_Key.txt file
 - Add the SpotifyAPI C# nuget package (Includes SpotifyAPI.Web.Auth.dll and SpotifyAPI.Web.dll)
 - Add the Newtonsoft.JSON nuget package
 - Add the libmp3lame nuget package
 - Build the project
 - Accept control for your Spotify session from your app
 
#### The highlight of this app is that it r e c o r d s the songs that play, downloads the cover art for them, and sets the .mp3 tags.  Please note that this is only for personal educational use to learn the Spotify web API and different C# audio tools, and that you take full responsibility for any repurcussions from Spotify.


## App screenshots

#### Main start screen
![Main start screen screenshot](Example_Pics/MainScreen.png)


#### Queue (set up by dragging tracks/albums/playlists from the Spotify web app)
![Queue screenshot](Example_Pics/Queue.png)

#### Playing screen
![Playing screen screenshot](Example_Pics/Playing.png)
