import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { authApi } from '../lib/api/auth';
import type { User } from '../lib/api/types';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<{ success: boolean; message?: string }>;
  logout: () => void;
  checkAuth: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const isAuthenticated = user !== null;
  const isAdmin = user?.isAdmin || false;

  const login = async (email: string, password: string) => {
    try {
      const response = await authApi.login(email, password);
      
      if (response.success && response.data) {
        const { token, user } = response.data;
        localStorage.setItem('hcbe_token', token);
        localStorage.setItem('hcbe_user', JSON.stringify(user));
        setUser(user);
        return { success: true };
      } else {
        return { success: false, message: response.message || 'Login failed' };
      }
    } catch (error) {
      console.error('Login error:', error);
      const errorMessage = error instanceof Error ? error.message : 'Login failed';
      return { success: false, message: errorMessage };
    }
  };

  const logout = () => {
    authApi.logout();
    setUser(null);
  };

  const checkAuth = async () => {
    setIsLoading(true);
    try {
      const token = localStorage.getItem('hcbe_token');
      if (!token) {
        setIsLoading(false);
        return;
      }

      const response = await authApi.getCurrentUser();
      if (response.success && response.data) {
        setUser(response.data);
        localStorage.setItem('hcbe_user', JSON.stringify(response.data));
      } else {
        // Token is invalid, clear it
        authApi.logout();
        setUser(null);
      }
    } catch (error) {
      console.error('Auth check error:', error);
      authApi.logout();
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    // Initialize auth state from localStorage
    const storedUser = localStorage.getItem('hcbe_user');
    if (storedUser) {
      try {
        setUser(JSON.parse(storedUser));
      } catch (error) {
        console.error('Error parsing stored user:', error);
        localStorage.removeItem('hcbe_user');
      }
    }
    
    // Check if the token is still valid
    checkAuth();
  }, []);

  const value: AuthContextType = {
    user,
    isAuthenticated,
    isAdmin,
    isLoading,
    login,
    logout,
    checkAuth
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};