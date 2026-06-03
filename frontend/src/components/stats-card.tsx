import { cn } from "@/lib/utils";

interface StatsCardProps {
  title: string;
  value: string;
  sub?: string;
  icon?: React.ReactNode;
  positive?: boolean;
}

export function StatsCard({ title, value, sub, icon, positive }: StatsCardProps) {
  return (
    <div className="rounded-lg border bg-card p-4 text-card-foreground">
      <div className="flex items-center justify-between">
        <p className="text-sm font-medium text-muted-foreground">{title}</p>
        {icon}
      </div>
      <p className={cn(
        "mt-2 text-2xl font-bold",
        positive === true && "text-green-500",
        positive === false && "text-red-500"
      )}>
        {value}
      </p>
      {sub && <p className="mt-1 text-xs text-muted-foreground">{sub}</p>}
    </div>
  );
}
