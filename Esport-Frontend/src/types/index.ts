export type PagedResponse<T> = {
  data: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

export type UserDto = {
  id: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string;
};

export type TournamentDto = {
  id: number;
  name: string;
  description: string;
  game: string;
  startDate: string;
  endDate: string;
  maxTeams: number;
  currentTeams: number;
  status: string;
  prizePool: number;
  isActive: boolean;
  createdAt: string;
  organizer?: UserDto | null;
};

export type CreateTournamentDto = {
  name: string;
  description: string;
  game: string;
  startDate: string;
  endDate: string;
  maxTeams: number;
  prizePool: number;
  organizerId: number;
};

export type UpdateTournamentDto = {
  name: string;
  description: string;
  startDate: string;
  endDate: string;
  maxTeams: number;
  prizePool: number;
  status: string;
};

export type TeamDto = {
  id: number;
  name: string;
  tag: string;
  description: string;
  region: string;
  isActive: boolean;
  createdAt: string;
  captain?: UserDto | null;
  players: PlayerSummaryDto[];
};

export type TeamSummaryDto = {
  id: number;
  name: string;
  tag: string;
  region: string;
  isActive: boolean;
  captain?: UserDto | null;
};

export type CreateTeamDto = {
  name: string;
  tag: string;
  description: string;
  region: string;
  captainId: number;
};

export type UpdateTeamDto = {
  name: string;
  tag: string;
  description: string;
  region: string;
};

export type PlayerDto = {
  id: number;
  nickname: string;
  position: string;
  country: string;
  age: number;
  totalMatches: number;
  wins: number;
  losses: number;
  winRate: number;
  ranking: number;
  isActive: boolean;
  joinedAt: string;
  user?: UserDto | null;
  team?: TeamSummaryDto | null;
};

export type PlayerSummaryDto = {
  id: number;
  nickname: string;
  position: string;
  country: string;
  isActive: boolean;
};

export type CreatePlayerDto = {
  nickname: string;
  position: string;
  country: string;
  age: number;
  userId: number;
  teamId?: number | null;
};

export type UpdatePlayerDto = {
  position: string;
  country: string;
  age: number;
  teamId?: number | null;
};

export type MatchPlayerDto = {
  id: number;
  kills: number;
  deaths: number;
  assists: number;
  champion: string;
  isStarter: boolean;
  player?: PlayerSummaryDto | null;
};

export type MatchDto = {
  id: number;
  scheduledAt: string;
  startedAt?: string | null;
  endedAt?: string | null;
  status: string;
  homeTeamScore: number;
  awayTeamScore: number;
  matchType: string;
  format: string;
  notes: string;
  createdAt: string;
  homeTeam?: TeamSummaryDto | null;
  awayTeam?: TeamSummaryDto | null;
  winnerTeam?: TeamSummaryDto | null;
  tournament?: TournamentDto | null;
  matchPlayers: MatchPlayerDto[];
};

export type CreateMatchDto = {
  tournamentId: number;
  homeTeamId: number;
  awayTeamId: number;
  scheduledAt: string;
  matchType?: string;
  format?: string;
  notes?: string;
};

export type UpdateMatchDto = {
  scheduledAt: string;
  status: string;
  homeTeamScore: number;
  awayTeamScore: number;
  winnerTeamId?: number | null;
  notes: string;
  startedAt?: string | null;
  endedAt?: string | null;
};

export type AuthResponseDto = {
  token: string;
  user: UserDto;
  expiresAt: string;
};

export type GameStatsDto = {
  game: string;
  count: number;
};

export type TournamentStatsDto = {
  totalTournaments: number;
  activeTournaments: number;
  completedTournaments: number;
  registrationOpen: number;
  totalPrizePool: number;
  popularGames: GameStatsDto[];
};
