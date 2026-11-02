"""Generate an ignored geometry snapshot from the developer's installed game.

No game data is downloaded. Tested against Steam build 26330345.
"""
from __future__ import annotations
import argparse
import json
from pathlib import Path
import numpy as np
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler

EXPECTED = {"Floor": 694, "ConcreteFloorCol": 40, "MetalFloorCol": 425}


def transform(game_object):
    return next(c.component.deref().read() for c in game_object.m_Component
                if c.component.deref().type.name in ("Transform", "RectTransform"))


def world_matrix(t, cache):
    key = t.object_reader.path_id
    if key in cache:
        return cache[key]
    p, s, q = t.m_LocalPosition, t.m_LocalScale, t.m_LocalRotation
    x, y, z, w = q.x, q.y, q.z, q.w
    rotation = np.array([
        [1 - 2*(y*y + z*z), 2*(x*y - z*w), 2*(x*z + y*w)],
        [2*(x*y + z*w), 1 - 2*(x*x + z*z), 2*(y*z - x*w)],
        [2*(x*z - y*w), 2*(y*z + x*w), 1 - 2*(x*x + y*y)],
    ])
    matrix = np.eye(4)
    matrix[:3, :3] = rotation @ np.diag([s.x, s.y, s.z])
    matrix[:3, 3] = [p.x, p.y, p.z]
    if t.m_Father.path_id:
        matrix = world_matrix(t.m_Father.deref().read(), cache) @ matrix
    cache[key] = matrix
    return matrix


def export(game_dir: Path):
    scene = game_dir / "Can 'Em All_Data" / "level0"
    if not scene.is_file():
        raise ValueError("Expected Can 'Em All_Data/level0 in the supplied game directory")
    env = UnityPy.load(str(scene))
    result, cache = [], {}
    for obj in env.objects:
        if obj.type.name not in ("MeshCollider", "MeshRenderer"):
            continue
        data = obj.read()
        game_object = data.m_GameObject.deref().read()
        name = game_object.m_Name
        if obj.type.name == "MeshCollider" and name not in ("ConcreteFloorCol", "MetalFloorCol"):
            continue
        if obj.type.name == "MeshRenderer" and name != "Floor":
            continue
        visual = obj.type.name == "MeshRenderer"
        if visual:
            mesh_filter = next(c.component.deref().read() for c in game_object.m_Component
                               if c.component.deref().type.name == "MeshFilter")
            mesh = mesh_filter.m_Mesh.deref().read()
            first = data.m_StaticBatchInfo.firstSubMesh
            count = data.m_StaticBatchInfo.subMeshCount
            if count <= 0:
                raise ValueError("Unsupported Floor renderer: expected the tested static batch layout")
        else:
            mesh = data.m_Mesh.deref().read()
            first, count = 0, len(mesh.m_SubMeshes)
        handler = MeshHandler(mesh)
        handler.process()
        triangles = handler.get_triangles()[first:first + count]
        ids = sorted({index for submesh in triangles for tri in submesh for index in tri})
        if len(ids) != EXPECTED[name]:
            raise ValueError(f"Unsupported {name} layout: {len(ids)} vertices; expected {EXPECTED[name]}")
        remap = {original: compact for compact, original in enumerate(ids)}
        matrix = np.eye(4) if visual else world_matrix(transform(game_object), cache)
        vertices = np.array(handler.m_Vertices)[ids] @ matrix[:3, :3].T + matrix[:3, 3]
        row = {"Name": name, "Kind": "visual" if visual else "collider",
               "Vertices": vertices.tolist(),
               "Submeshes": [[remap[index] for tri in submesh for index in tri]
                             for submesh in triangles]}
        if visual:
            for key, values, length in (("Normals", handler.m_Normals, 3),
                                        ("UV", handler.m_UV0, 2),
                                        ("UV2", handler.m_UV1, 2),
                                        ("Tangents", handler.m_Tangents, 4)):
                if values:
                    row[key] = [list(values[index])[:length] for index in ids]
        result.append(row)
        print(f"{name}: {len(ids)} vertices")
    if len(result) != 3 or {row["Name"] for row in result} != set(EXPECTED):
        raise ValueError("Expected exactly one Floor renderer and the two tested floor colliders")
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-dir", type=Path, required=True)
    default = Path(__file__).resolve().parents[1] / "src" / "PermanentUpgradesRevamped" / "FloorGeometry.json"
    parser.add_argument("--output", type=Path, default=default)
    parser.add_argument("--force", action="store_true", help="Replace a previous local snapshot")
    args = parser.parse_args()
    if args.output.exists() and not args.force:
        parser.error("Output already exists; use --force to regenerate the local snapshot")
    result = export(args.game_dir.resolve())
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, separators=(",", ":")), encoding="utf-8")
    print("Local floor snapshot generated. Keep it out of public commits and releases.")


if __name__ == "__main__":
    main()
