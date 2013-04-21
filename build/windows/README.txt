== Building ==

See http://banshee.fm/download/development/#windows for instructions for building
Banshee on Windows.

== Creating the Banshee.msi installer ==

You need
- WIX 3.5 installed
- Banshee built

With that, you should be able to run build-installer.js (inside a Windows command
line shell, not simply running it in an explorer window) and have it produce the
installer.

== Maintenance ==

To update the bundled dependencies, use the bundle-deps.bat script, which will
copy Gtk# and GStreamer into Banshee's bin/ directory.  It only needs to be run
by maintainers updating the bundled deps. See the script for which packages you
need to have installed.

Before packaging a release, make sure always that the post-build.bat script is
updated (via running ./update-scripts); otherwise the build will fail or have
some resources missing. (And don't forget to commit the changes.)
