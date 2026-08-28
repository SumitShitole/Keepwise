import type { Metadata } from "next";
import { Geist } from "next/font/google";
import "./globals.css";

const geist = Geist({ subsets: ["latin"], variable: "--font-geist-sans" });

export const metadata: Metadata = {
  title: "Keepwise",
  description: "Track warranties, maintenance, and renewals so important dates never slip by."
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body className={`${geist.variable} min-h-screen bg-zinc-50 text-zinc-900 antialiased`}>
        {children}
      </body>
    </html>
  );
}
