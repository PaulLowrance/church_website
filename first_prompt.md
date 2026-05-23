# Church Website Initial Prompt

The purpose of this site is to provide administrators and church leadership an easy way to maintain the information about the church, promote upcoming events, host the sermon audio, and provide the sermons as a podcast. The motivation for this is to update the very aged `http://bhpbc.org` which is built on a deprecated django platform.

## Tech Stack
- Application backend Code: 
    * Resides in `server` directory at the repository root
    * dotnet 10+
    * Fast Endpoints for API, 
    * Dapper for ORM
    * PostgreSQL DB (container)
    * JWT authentication, with role-base authorization.
- Application UI Code: 
    * Resides in `app` directory at the repository root
    * Vue3 (Composition API)
    * Vite
    * Quasar controls
    * Pinia for state control

## Prioritized functional goals for the site:
1. Provide a way to create & manage static site content for the church via an admin panel. (examples "Home" page, "About Us" page, Header & Footer content)
    * Static pages should follow the UI theme of the site and should be built from one or more templates. 
    * The body content with in should be able to be added or modified from the admin panel by an admin and shouldn't require a deployment of new code.
    * The content editing in the admin panel should allow the user toi choose Markdown or HTML for styling the body content.
2. Provide a page to see the sermons that have been uploaded to the site with a clean UI that contains the speaker name, date & time published, and the ability to listen to the audio on the page or download for listening later.
    * The sermon list should be maintained via an option in the admin menu.
    * When uploading a sermon audio file the following are required:
        - Sermon Name
        - Speaker Name
        - Date & Time to publish
        - Uploaded audio file
    * The date uploaded should be set automatically when the sermon entry is saved
    * Additional fields that should be present but are optional are:
        - Description
        - Series Name
    * There should also be a way to apply tags to the sermon entries
    * The audio file should be stored on the server, not in the container and the path to the storage location should be configurable in appsettings.
3. The sermons list is a feed for a podcast and that functionality must be duplicated but remain at the same path and the same structure as found here at http://bhpbc.org/podcast/rss/.
4. Accessability is important to the site. Ensure that the controls and UI elements are following the Web Content Accessibility Guidelines (WCAG).

## Technical goals for the site:
1. Use containers for deployment. 
    - The application code and database should be in their own containers and the whole stack should be deployable via `docker compose` commands.
    - Use Aspire for local development of containers
2. The site should be mobile-friendly in that the selection of controls and styling should be such that the site can be used well on a mobile phone browser and on a laptop/desktop browser.
3. Deployment to production should be handled through a deployment pipeline action (preferably GitHub actions)






