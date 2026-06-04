import type { Metadata } from "next";
import { Geist } from "next/font/google";
import "./globals.css";
import { ApolloClientProvider } from "@/components/apollo-provider";
import { ThemeProvider } from "@/components/theme-provider";
import { Sidebar, MobileBar, MobileNav } from "@/components/sidebar";

const geist = Geist({ variable: "--font-geist-sans", subsets: ["latin"] });

export const metadata: Metadata = {
  title: "ParisSpo — Paris Football Intelligent",
  description: "Suggestions IA, value betting, Kelly Criterion",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="fr" className={geist.variable} suppressHydrationWarning>
      <body className="min-h-screen bg-background text-foreground">
        <ThemeProvider>
          <ApolloClientProvider>
            <Sidebar />
            <div className="lg:pl-60">
              <MobileBar />
              <main className="px-4 py-6 pb-24 lg:px-8 lg:py-8 lg:pb-8 max-w-[1400px] mx-auto">
                {children}
              </main>
            </div>
            <MobileNav />
          </ApolloClientProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
