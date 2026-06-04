"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";
import { useTheme } from "@/components/theme-provider";
import { Trophy, History, Wallet, Calculator, LayoutDashboard, Sun, Moon, Zap } from "lucide-react";

const links = [
  { href: "/",           label: "Dashboard",  icon: LayoutDashboard },
  { href: "/paris",      label: "Paris",      icon: Trophy },
  { href: "/historique", label: "Historique", icon: History },
  { href: "/bankroll",   label: "Bankroll",   icon: Wallet },
  { href: "/simulateur", label: "Simulateur", icon: Calculator },
];

export function Nav() {
  const pathname = usePathname();
  const { theme, toggle } = useTheme();

  return (
    <header className="sticky top-0 z-50 border-b border-border/60 bg-background/80 backdrop-blur-xl">
      <div className="container mx-auto flex h-14 items-center justify-between px-4">

        {/* Logo */}
        <Link href="/" className="flex items-center gap-2 font-bold text-base tracking-tight">
          <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-primary text-primary-foreground text-sm">
            <Zap className="h-4 w-4" />
          </span>
          <span>Paris<span className="text-primary">Spo</span></span>
        </Link>

        {/* Nav links */}
        <nav className="hidden md:flex items-center gap-1">
          {links.map(({ href, label, icon: Icon }) => {
            const active = pathname === href;
            return (
              <Link
                key={href}
                href={href}
                className={cn(
                  "flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm font-medium transition-all duration-150",
                  active
                    ? "bg-primary/15 text-primary"
                    : "text-muted-foreground hover:bg-secondary hover:text-foreground"
                )}
              >
                <Icon className="h-3.5 w-3.5" />
                {label}
              </Link>
            );
          })}
        </nav>

        {/* Theme toggle */}
        <button
          onClick={toggle}
          className="flex h-8 w-8 items-center justify-center rounded-lg border border-border bg-secondary text-muted-foreground hover:text-foreground hover:bg-accent transition-all duration-150"
          aria-label="Toggle theme"
        >
          {theme === "dark" ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
        </button>

      </div>
    </header>
  );
}
