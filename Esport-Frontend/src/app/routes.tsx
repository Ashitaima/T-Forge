import { createBrowserRouter } from "react-router-dom";
import { AppShell } from "../components/layout/AppShell";
import { ProtectedRoute } from "../components/ProtectedRoute";

export const router = createBrowserRouter([
  {
    element: <AppShell />,
    children: [
      {
        index: true,
        lazy: async () => ({
          Component: (await import("../features/dashboard/DashboardPage")).default
        })
      },
      {
        path: "tournaments",
        lazy: async () => ({
          Component: (await import("../features/tournaments/TournamentList")).default
        })
      },
      {
        path: "tournaments/:id",
        lazy: async () => ({
          Component: (await import("../features/tournaments/TournamentDetail")).default
        })
      },
      {
        element: <ProtectedRoute roles={["Organizer"]} />,
        children: [
          {
            path: "tournaments/new",
            lazy: async () => ({
              Component: (await import("../features/tournaments/TournamentForm")).default
            })
          },
          {
            path: "tournaments/:id/edit",
            lazy: async () => ({
              Component: (await import("../features/tournaments/TournamentForm")).default
            })
          },
          {
            path: "matches/new",
            lazy: async () => ({
              Component: (await import("../features/matches/MatchForm")).default
            })
          },
          {
            path: "matches/:id/edit",
            lazy: async () => ({
              Component: (await import("../features/matches/MatchForm")).default
            })
          }
        ]
      },
      {
        element: <ProtectedRoute />,
        children: [
          {
            path: "teams/new",
            lazy: async () => ({
              Component: (await import("../features/teams/TeamForm")).default
            })
          },
          {
            path: "teams/:id/edit",
            lazy: async () => ({
              Component: (await import("../features/teams/TeamForm")).default
            })
          },
          {
            path: "players/new",
            lazy: async () => ({
              Component: (await import("../features/players/PlayerForm")).default
            })
          },
          {
            path: "players/:id/edit",
            lazy: async () => ({
              Component: (await import("../features/players/PlayerForm")).default
            })
          }
        ]
      },
      {
        element: <ProtectedRoute roles={["Admin"]} />,
        children: [
          {
            path: "users/new",
            lazy: async () => ({
              Component: (await import("../features/users/UserForm")).default
            })
          },
          {
            path: "users/:id/edit",
            lazy: async () => ({
              Component: (await import("../features/users/UserForm")).default
            })
          },
          {
            path: "users",
            lazy: async () => ({
              Component: (await import("../features/users/UsersList")).default
            })
          }
        ]
      },
      {
        path: "teams",
        lazy: async () => ({
          Component: (await import("../features/teams/TeamsList")).default
        })
      },
      {
        path: "players",
        lazy: async () => ({
          Component: (await import("../features/players/PlayersList")).default
        })
      },
      {
        path: "teams/:id",
        lazy: async () => ({
          Component: (await import("../features/teams/TeamDetail")).default
        })
      },
      {
        path: "matches",
        lazy: async () => ({
          Component: (await import("../features/matches/MatchesSchedule")).default
        })
      },
      
    ]
  },
  {
    path: "login",
    lazy: async () => ({
      Component: (await import("../features/auth/LoginPage")).default
    })
  },
  {
    path: "register",
    lazy: async () => ({
      Component: (await import("../features/auth/RegisterPage")).default
    })
  }
]);
