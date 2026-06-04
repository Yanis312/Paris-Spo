import { cn } from "@/lib/utils";

type Accent = "green" | "blue" | "amber" | "purple" | "red";

interface StatsCardProps {
  title: string;
  value: string;
  sub?: string;
  icon?: React.ReactNode;
  accent?: Accent;
  positive?: boolean;
}

const accentVar: Record<Accent, string> = {
  green: "var(--green)",
  blue: "var(--blue)",
  amber: "var(--amber)",
  purple: "var(--purple)",
  red: "var(--red)",
};

export function StatsCard({ title, value, sub, icon, accent = "green", positive }: StatsCardProps) {
  const color = accentVar[accent];
  return (
    <div className="card-base card-hover relative overflow-hidden p-5">
      {/* corner glow */}
      <div
        className="pointer-events-none absolute -right-8 -top-8 h-24 w-24 rounded-full blur-2xl opacity-20"
        style={{ background: color }}
      />
      <div className="relative flex items-start justify-between">
        <div className="space-y-1">
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">{title}</p>
          <p
            className="text-2xl md:text-3xl font-bold tracking-tight"
            style={{ color: positive === true ? "var(--green)" : positive === false ? "var(--red)" : undefined }}
          >
            {value}
          </p>
        </div>
        <span
          className="flex h-10 w-10 items-center justify-center rounded-xl shrink-0"
          style={{ background: `color-mix(in srgb, ${color} 14%, transparent)`, color }}
        >
          {icon}
        </span>
      </div>
      {sub && <p className="relative mt-2 text-xs text-muted-foreground">{sub}</p>}
    </div>
  );
}
