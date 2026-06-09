"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";
import { useTheme } from "@/components/theme-provider";
import { Icon } from "@/components/icon";

const sections = [
  {
    title: "Principal",
    links: [
      { href: "/",           label: "Dashboard",  icon: "dashboard" },
      { href: "/calendrier", label: "Calendrier WC", icon: "trophy" },
      { href: "/paris",      label: "Paris du jour", icon: "bolt" },
    ],
  },
  {
    title: "Gestion",
    links: [
      { href: "/historique", label: "Historique",  icon: "history" },
      { href: "/bankroll",   label: "Bankroll",    icon: "wallet" },
      { href: "/simulateur", label: "Simulateur",  icon: "calculator" },
      { href: "/simulation", label: "Simulation /jour", icon: "trend" },
    ],
  },
];

export function Sidebar() {
  const pathname = usePathname();
  const { theme, toggle } = useTheme();

  return (
    <aside className="fixed inset-y-0 left-0 z-40 hidden w-60 flex-col border-r border-border bg-card/40 backdrop-blur-xl lg:flex">
      {/* Logo */}
      <div className="flex h-16 items-center gap-2.5 border-b border-border px-5">
        <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-[var(--grad-1)] to-[var(--grad-2)] shadow-lg shadow-[var(--primary)]/20">
          <Icon name="football" className="text-white text-base" />
        </span>
        <div className="flex flex-col leading-none">
          <span className="font-bold text-base tracking-tight">
            Paris<span className="gradient-text">Spo</span>
          </span>
          <span className="text-[10px] text-muted-foreground mt-0.5">Football IA</span>
        </div>
      </div>

      {/* Nav sections */}
      <nav className="flex-1 overflow-y-auto px-3 py-5 space-y-6">
        {sections.map((section) => (
          <div key={section.title}>
            <p className="px-3 mb-2 text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/70">
              {section.title}
            </p>
            <div className="space-y-1">
              {section.links.map(({ href, label, icon }) => {
                const active = pathname === href;
                return (
                  <Link
                    key={href}
                    href={href}
                    className={cn(
                      "group relative flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-all duration-150",
                      active
                        ? "bg-primary/10 text-primary"
                        : "text-muted-foreground hover:bg-secondary hover:text-foreground"
                    )}
                  >
                    {active && (
                      <span className="absolute left-0 top-1/2 h-5 w-1 -translate-y-1/2 rounded-r-full bg-primary" />
                    )}
                    <Icon name={icon} className={cn("w-4 text-center transition-transform group-hover:scale-110", active && "text-primary")} />
                    {label}
                  </Link>
                );
              })}
            </div>
          </div>
        ))}
      </nav>

      {/* Footer: theme toggle */}
      <div className="border-t border-border p-3">
        <button
          onClick={toggle}
          className="flex w-full items-center justify-between rounded-xl px-3 py-2.5 text-sm font-medium text-muted-foreground hover:bg-secondary hover:text-foreground transition-all"
        >
          <span className="flex items-center gap-3">
            <Icon name={theme === "dark" ? "moon" : "sun"} className="w-4 text-center" />
            {theme === "dark" ? "Mode sombre" : "Mode clair"}
          </span>
          <span className="relative h-5 w-9 rounded-full bg-secondary border border-border">
            <span className={cn(
              "absolute top-0.5 h-3.5 w-3.5 rounded-full bg-primary transition-all duration-200",
              theme === "dark" ? "left-0.5" : "left-[18px]"
            )} />
          </span>
        </button>
      </div>
    </aside>
  );
}

/* Mobile top bar */
export function MobileBar() {
  const { theme, toggle } = useTheme();
  return (
    <div className="sticky top-0 z-40 flex h-14 items-center justify-between border-b border-border bg-card/60 backdrop-blur-xl px-4 lg:hidden">
      <Link href="/" className="flex items-center gap-2 font-bold">
        <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-gradient-to-br from-[var(--grad-1)] to-[var(--grad-2)]">
          <Icon name="football" className="text-white text-sm" />
        </span>
        Paris<span className="gradient-text">Spo</span>
      </Link>
      <button onClick={toggle} className="flex h-8 w-8 items-center justify-center rounded-lg border border-border bg-secondary text-muted-foreground">
        <Icon name={theme === "dark" ? "sun" : "moon"} className="w-4 text-center" />
      </button>
    </div>
  );
}

/* Mobile bottom nav */
export function MobileNav() {
  const pathname = usePathname();
  const allLinks = sections.flatMap(s => s.links);
  return (
    <nav className="fixed bottom-0 inset-x-0 z-40 flex items-center justify-around border-t border-border bg-card/80 backdrop-blur-xl px-2 py-2 lg:hidden">
      {allLinks.map(({ href, label, icon }) => {
        const active = pathname === href;
        return (
          <Link key={href} href={href} className={cn(
            "flex flex-col items-center gap-0.5 rounded-lg px-3 py-1.5 text-[10px] font-medium transition-colors",
            active ? "text-primary" : "text-muted-foreground"
          )}>
            <Icon name={icon} className="w-4 text-center" />
            {label.split(" ")[0]}
          </Link>
        );
      })}
    </nav>
  );
}
