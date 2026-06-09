"use client";

import { config } from "@fortawesome/fontawesome-svg-core";
import "@fortawesome/fontawesome-svg-core/styles.css";
config.autoAddCss = false;
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import {
  faGaugeHigh, faTrophy, faClockRotateLeft, faWallet, faCalculator,
  faChartLine, faBolt, faBullseye, faMoneyBillTrendUp, faClock,
  faCircleCheck, faCircleXmark, faTrash, faArrowTrendUp, faArrowTrendDown,
  faShieldHalved, faWandMagicSparkles, faPlay, faPlus, faArrowsRotate,
  faSun, faMoon, faCalendarDays, faFutbol, faFireFlameCurved, faDollarSign,
} from "@fortawesome/free-solid-svg-icons";
import type { IconDefinition } from "@fortawesome/fontawesome-svg-core";

const MAP: Record<string, IconDefinition> = {
  dashboard: faGaugeHigh,
  trophy: faTrophy,
  history: faClockRotateLeft,
  wallet: faWallet,
  calculator: faCalculator,
  chart: faChartLine,
  bolt: faBolt,
  target: faBullseye,
  trend: faMoneyBillTrendUp,
  clock: faClock,
  check: faCircleCheck,
  xmark: faCircleXmark,
  trash: faTrash,
  up: faArrowTrendUp,
  down: faArrowTrendDown,
  shield: faShieldHalved,
  sparkles: faWandMagicSparkles,
  play: faPlay,
  plus: faPlus,
  refresh: faArrowsRotate,
  sun: faSun,
  moon: faMoon,
  calendar: faCalendarDays,
  football: faFutbol,
  fire: faFireFlameCurved,
  dollar: faDollarSign,
};

export function Icon({ name, className, spin }: { name: keyof typeof MAP | string; className?: string; spin?: boolean }) {
  const def = MAP[name] ?? faBolt;
  return <FontAwesomeIcon icon={def} className={className} spin={spin} />;
}
