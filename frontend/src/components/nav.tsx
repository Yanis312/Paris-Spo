"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";
import { Trophy, History, Wallet, Calculator, LayoutDashboard } from "lucide-react";

const links = [
  { href: "/", label: "Dashboard", icon: LayoutDashboard },
  { href: "/paris", label: "Paris", icon: Trophy },
  { href: "/historique", label: "Historique", icon: History },
  { href: "/bankroll", label: "Bankroll", icon: Wallet },
  { href: "/simulateur", label: "Simulateur", icon: Calculator },
];

export function Nav() {
  const pathname = usePathname();
  return (
    <nav className="border-b bg-background">
      <div className="container mx-auto flex h-14 items-center gap-6 px-4">
        <span className="font-bold text-lg">⚽ Paris-Spo</span>
        <div className="flex gap-1">
          {links.map(({ href, label, icon: Icon }) => (
            <Link
              key={href}
              href={href}
              className={cn(
                "flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground",
                pathname === href ? "bg-accent text-accent-foreground" : "text-muted-foreground"
              )}
            >
              <Icon className="h-4 w-4" />
              {label}
            </Link>
          ))}
        </div>
      </div>
    </nav>
  );
}
