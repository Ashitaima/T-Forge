import { ReactNode } from "react";

type StatCardProps = {
  title: string;
  value: string;
  trend?: string;
  icon?: ReactNode;
};

export const StatCard = ({ title, value, trend, icon }: StatCardProps) => {
  return (
    <div className="glass-panel rounded-2xl p-5">
      <div className="flex items-center justify-between text-sm text-slate-400">
        <span>{title}</span>
        {icon}
      </div>
      <div className="mt-3 text-3xl font-semibold text-white">{value}</div>
      {trend && <div className="mt-2 text-xs text-neon-green">{trend}</div>}
    </div>
  );
};
