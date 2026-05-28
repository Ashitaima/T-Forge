/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        night: {
          900: "#0a0c12",
          800: "#0f131d",
          700: "#151b2b"
        },
        neon: {
          cyan: "#20f6ff",
          magenta: "#ff3bf6",
          green: "#45ff8b",
          amber: "#ffb020"
        }
      },
      boxShadow: {
        neon: "0 0 20px rgba(32, 246, 255, 0.35)",
        glow: "0 0 40px rgba(255, 59, 246, 0.25)"
      }
    }
  },
  plugins: []
};
