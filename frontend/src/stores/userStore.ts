import { create } from 'zustand';

interface UserStore {
  currentUserId: number | null;
  setCurrentUserId: (id: number) => void;
}

export const useUserStore = create<UserStore>((set) => ({
  currentUserId: null,
  setCurrentUserId: (id) => set({ currentUserId: id }),
}));
