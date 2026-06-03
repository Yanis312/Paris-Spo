import type { Metadata } from "next";
import { Geist } from "next/font/google";
import "./globals.css";
import { ApolloClientProvider } from "@/components/apollo-provider";
import { Nav } from "@/components/nav";

const geist = Geist({ variable: "--font-geist-sans", subsets: ["latin"] });

export const metadata: Metadata = {
  title: "Paris-Spo — Paris Football Intelligent",
  description: "Suggestions IA, value betting, Kelly Criterion",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="fr" className={`${geist.variable} h-full antialiased dark`}>
      <body className="min-h-full flex flex-col bg-background text-foreground">
        <ApolloClientProvider>
          <Nav />
          <main className="container mx-auto flex-1 px-4 py-6">{children}</main>
        </ApolloClientProvider>
      </body>
    </html>
  );
}
