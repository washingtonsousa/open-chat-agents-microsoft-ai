"use client";

import { createContext, useContext, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Box, CircularProgress } from "@mui/material";
import { authApi, authStorage } from "@/services/api";
import type { User } from "@/types";
import { ChangePasswordModal } from "./ChangePasswordModal";

const AuthContext = createContext<User | null>(null);

export function useCurrentUser() {
  return useContext(AuthContext);
}

export function AuthGuard({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [mustChangePassword, setMustChangePassword] = useState(false);

  useEffect(() => {
    if (!authStorage.getToken()) {
      router.replace("/login");
      return;
    }
    authApi
      .me()
      .then((u) => {
        setUser(u);
        setMustChangePassword(u.must_change_password);
      })
      .catch(() => router.replace("/login"))
      .finally(() => setLoading(false));
  }, [router]);

  if (loading || !user) {
    return (
      <Box sx={{ display: "flex", height: "100%", alignItems: "center", justifyContent: "center" }}>
        <CircularProgress size={28} />
      </Box>
    );
  }

  if (mustChangePassword) {
    return (
      <ChangePasswordModal
        forced
        onDone={() => setMustChangePassword(false)}
      />
    );
  }

  return <AuthContext.Provider value={user}>{children}</AuthContext.Provider>;
}
