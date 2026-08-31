const parseBooleanEnv = (value: string | undefined, defaultValue: boolean): boolean => {
  if (value === undefined || value === '') {
    return defaultValue;
  }

  return value === 'true' || value === '1';
};

export const features = {
  /** Affiche le formulaire de connexion sur la page Espace Membre */
  memberLoginEnabled: parseBooleanEnv(import.meta.env.VITE_ENABLE_MEMBER_LOGIN, true),
  /** Affiche le module Équipe dans l'espace admin */
  adminTeamMembersEnabled: parseBooleanEnv(import.meta.env.VITE_ENABLE_ADMIN_TEAM_MEMBERS, true),
} as const;
