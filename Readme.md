  # 🏗️ The Grocery-Service Project Guide 


   Think of your entire project as a highly organized Restaurant.

   ---

   ## The 4-Tier Architecture

   When you ask for a "strict 4-tier architecture", Every folder has a very strict job, and they are only allowed to talk to specific people.

   ### 1. `Grocery.DMO` (Data Model Objects) 🍅
   **Analogy:** The Raw Ingredients.
   - **What it does:** This folder just holds simple definitions of what your data looks like *exactly* as it is stored in the database.
   - **The Code:** `GroceryProduct.cs` and `User.cs`. They just list properties like `Name`, `FatContent`, and `StockQuantity`.
   - **Rules:** It doesn't know about any other folders. It just exists to define shapes. **Relationship:** One to One with serv hib.

   ### 2. `Grocery.Context` (Data Access Layer) 👨‍🍳
   **Analogy:** The Chef in the Kitchen.
   - **What it does:** This is the ONLY folder that is allowed to talk to the Database (MongoDB). 
   - **The Code:** `GroceryRepository.cs` and `UserRepository.cs`. 
   - **Rules:** If anyone wants to save a new product or look up a user, they *must* ask the `Grocery.Context`. The Context reaches into the database, grabs the raw ingredients (`DMO`), and passes them back. **Relationship:** One to One with serv hib.

   ### 3. `Grocery.DTO` (Data Transfer Objects) 🍽️
   **Analogy:** The Plated Meal.
   - **What it does:** When a customer orders food, you don't bring them a raw egg and flour. You bring them a baked cake. The DTO defines what the data looks like *after* we prepare it for the outside world.
   - **The Code:** `GroceryProductResponse.cs`.
   - **Rules:** Notice how we have an Admin DTO and a User DTO? That's because if an Admin asks for data, we give them a plate with everything (including `StockQuantity`). If a normal User asks, we give them a smaller plate that hides the stock and temperatures. **Relationship:** One to Many.

   ### 4. `Grocery.ServiceHub` (The Core Web API) 🤵
   **Analogy:** The Waiter & The Front Doors.
   - **What it does:** This is the actual application that runs. It listens for requests from the internet (like someone logging in from the website).
   - **The Code:** `Program.cs` and the `wwwroot` (HTML/CSS) folder.
   - **Rules:** The Waiter (`Program.cs`) gets a request from the user. The Waiter runs back to the Chef (`Context`), asks for the raw data (`DMO`), plates it nicely into a safe format (`DTO`), and carries it back out to the customer on the website (`wwwroot`).

   ---

   ## How it All Connects (The Flow)

   Let's trace exactly what happens when you type `admin` and `admin123` into the login page and click "Login".

   1. **The Website (`wwwroot/index.html`)**
      - The Javascript (`app.js`) takes your username and password and sends an HTTP request to the Waiter (`ServiceHub`).
   2. **The Waiter (`Grocery.ServiceHub/Program.cs`)**
      - The Waiter receives the request at the `/api/auth/login` endpoint.
      - The Waiter goes to the Chef (`UserRepository` in `Context`) and says: *"Hey, do we have a user with this password?"*
   3. **The Chef (`Grocery.Context/UserRepository.cs`)**
      - The Chef connects to MongoDB, searches the `users` table, and finds the raw ingredient (`User DMO`). 
      - The Chef hands the `User DMO` back to the Waiter.
   4. **The Waiter (`Grocery.ServiceHub/Program.cs`)**
      - The Waiter sees that the User is valid and their Role is "Admin".
      - The Waiter creates a **JWT Token** (like a VIP wristband) and hands it back to the Javascript.
   5. **The Website (`wwwroot/app.js`)**
      - The Javascript saves the VIP wristband in your browser's memory and redirects you to `dashboard.html`.

   Later, when the dashboard asks to see the products, it shows its VIP wristband to the Waiter. The Waiter sees the "Admin" role, goes to the Chef, gets all the products, plates them using the `GroceryProductAdminResponse` DTO (which includes secret stock numbers), and sends it to the screen!

   ---

   ## What is Docker Doing? 🐳
   Docker is like a shipping container. Instead of installing MongoDB, .NET, and everything manually on every single computer you own, I wrote a `Dockerfile` and a `docker-compose.yml`. 
   - **Docker-Compose** automatically spins up a "mini computer" just for the database, and another "mini computer" just for your code. It links them together so they can talk. 
   - That is why you only had to run ONE command (`docker-compose up`) to get the entire database, website, and API running simultaneously!
