/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        // Ковадло: холодний залізний ґрунт, на якому працює акцент
        ink: {
          950: "#0B0E12",
          900: "#12161D",
          800: "#191F28",
          700: "#212936"
        },
        line: {
          DEFAULT: "#2A3340",
          soft: "#1E2530"
        },
        // Розпечений метал — єдиний акцент бренду
        ember: {
          DEFAULT: "#FF6B1A",
          soft: "#FF9553",
          dim: "#B24A12"
        },
        // Статусні кольори — лише для стану, ніколи для декору
        win: "#4ADE80",
        danger: "#F4574D",
        text: {
          DEFAULT: "#E7ECF3",
          muted: "#8C99AB",
          faint: "#5D6979"
        }
      },
      fontFamily: {
        display: ['"Unbounded"', "system-ui", "sans-serif"],
        sans: ['"IBM Plex Sans"', "system-ui", "sans-serif"],
        mono: ['"IBM Plex Mono"', "ui-monospace", "monospace"]
      },
      fontSize: {
        // Свідома типографічна шкала замість випадкових text-sm/xs
        eyebrow: ["0.6875rem", { lineHeight: "1rem", letterSpacing: "0.14em" }],
        micro: ["0.75rem", { lineHeight: "1.1rem" }],
        body: ["0.875rem", { lineHeight: "1.35rem" }],
        lead: ["1rem", { lineHeight: "1.6rem" }],
        h3: ["1.125rem", { lineHeight: "1.5rem", letterSpacing: "-0.01em" }],
        h2: ["1.375rem", { lineHeight: "1.75rem", letterSpacing: "-0.015em" }],
        h1: ["1.875rem", { lineHeight: "2.25rem", letterSpacing: "-0.02em" }],
        display: ["2.5rem", { lineHeight: "2.75rem", letterSpacing: "-0.03em" }]
      },
      boxShadow: {
        raise: "0 1px 0 0 rgba(255,255,255,0.04) inset, 0 8px 24px -12px rgba(0,0,0,0.8)",
        heat: "0 0 0 1px rgba(255,107,26,0.35), 0 8px 30px -10px rgba(255,107,26,0.45)"
      },
      keyframes: {
        "heat-pulse": {
          "0%, 100%": { opacity: "0.55" },
          "50%": { opacity: "1" }
        }
      },
      animation: {
        "heat-pulse": "heat-pulse 2.4s ease-in-out infinite"
      }
    }
  },
  plugins: []
};
