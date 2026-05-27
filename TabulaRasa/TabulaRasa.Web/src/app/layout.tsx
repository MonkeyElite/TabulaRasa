import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "TabulaRasa Simulation",
  description: "Simulation control and state editor"
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
