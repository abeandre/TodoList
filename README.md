# ToDo List Application

This project is a full-stack ToDo application featuring a **C# ASP.NET Core Web API** backend and a **Vue 3 + TypeScript** frontend.

## Project Structure

- `ToDoApi`: The REST API backend handling routing and controllers.
- `ToDo.DataAccess`: The data access layer utilizing Entity Framework Core.
- `frontend`: The modern Vue 3 interface styled with scoped CSS.
- `ToDoApi.Tests` / `ToDo.DataAccess.Tests`: Automated backend unit testing frameworks.

---

## How to Run the Application

To experience the full functionality of the application, you must run **both** the API and the frontend development server concurrently. 

> **Note:** The Vue application uses Vite's proxy feature to automatically route all interface requests starting with `/api` to the C# backend. This prevents CORS policy issues.

### 1. Start the Backend API

Open a terminal, navigate to the `ToDoApi` directory, and run the project:

```bash
cd ToDoApi
dotnet run
```

*The backend API will initialize and actively listen on `http://localhost:5128`.*

### 2. Start the Vue Frontend

Open a **second terminal window**, navigate to the `frontend` directory, install Node packages (if it's your first time), and launch the server:

```bash
cd frontend
npm install
npm run dev
```

*The built-in Vite server will start and provide a local interface URL, typically `http://localhost:5173` (or `http://localhost:5175` if the default port is in use).*

### 3. View the App!

Open your web browser and navigate to the frontend URL provided in step 2. You will now be able to add, edit, finish, and delete ToDo tasks while the data synchronizes robustly with the API perfectly.

---

## Running Automated Tests

### Backend Unit Tests (C# xUnit)
To verify the accuracy and integrity of the API endpoints, use the `dotnet test` command:
```bash
cd ToDoApi.Tests
dotnet test
```

### Frontend Unit Tests (Vitest)
To verify Vue component functionalities logic and DOM rendering rules:
```bash
cd frontend
npm run test:unit
```

### Frontend E2E testing (Playwright)
*(Requires the frontend and backend servers to be running simultaneously)*
To simulate full end-to-end user browser interactions, trigger Playwright checks:
```bash
cd frontend
npx playwright test
```
*(Optionally, you can append `--project=chromium` if you only wish to test the Chrome engine).*
