"use client";

import { useEffect, useState } from "react";
import { Autocomplete, Chip, TextField } from "@mui/material";
import ExtensionIcon from "@mui/icons-material/Extension";
import { mcpServerApi } from "@/services/api";
import type { McpServer } from "@/types";

interface Props {
  value: string[];
  onChange: (ids: string[]) => void;
}

export function McpServerMultiSelect({ value, onChange }: Props) {
  const [options, setOptions] = useState<McpServer[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    mcpServerApi
      .list()
      .then((res) => setOptions(res.mcp_servers))
      .finally(() => setLoading(false));
  }, []);

  const selected = options.filter((server) => value.includes(server.id));

  return (
    <Autocomplete
      multiple
      loading={loading}
      options={options}
      value={selected}
      getOptionLabel={(server) => server.name}
      isOptionEqualToValue={(a, b) => a.id === b.id}
      onChange={(_, newValue) => onChange(newValue.map((server) => server.id))}
      noOptionsText="Nenhum servidor MCP cadastrado ainda."
      renderValue={(tagValue, getItemProps) =>
        tagValue.map((server, index) => {
          const { key, ...itemProps } = getItemProps({ index });
          return (
            <Chip
              {...itemProps}
              key={key}
              label={server.name}
              size="small"
              icon={<ExtensionIcon />}
              color="secondary"
              variant="outlined"
            />
          );
        })
      }
      renderInput={(params) => (
        <TextField
          {...params}
          placeholder={selected.length === 0 ? "Adicionar servidor MCP..." : undefined}
          size="small"
        />
      )}
    />
  );
}
