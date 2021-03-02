const gulp = require("gulp");
const del = require("del");
const sourcemaps = require("gulp-sourcemaps");
const terser = require("gulp-terser");
const ts = require("gulp-typescript");
const pathResolver = require("gulp-typescript-path-resolver");
const sass = require("gulp-sass")

const tsProject = ts.createProject("tsconfig.json")

function clean() {
    return del([
        "wwwroot/ts/**",
        "Pages/**/*.css",
        "Shared/**/*.css"
    ]);
}

function buildSass() {
    return gulp.src([
            "Pages/**/*.scss",
            "Shared/**/*.scss"
        ], { base: "./" })
        .pipe(sass().on("error", sass.logError))
        .pipe(gulp.dest("."));
}

function buildTs() {
    return tsProject.src()
        //.pipe(gulp.dest(tsProject.config.compilerOptions.outDir))
        .pipe(sourcemaps.init())
        .pipe(tsProject())
        .pipe(pathResolver.tsPathResolver(tsProject.config.compilerOptions, {}))
        .pipe(terser())
        .pipe(sourcemaps.write("."))
        .pipe(gulp.dest(tsProject.config.compilerOptions.outDir, { sourcemaps: "." }));
}

exports.default = gulp.series(clean, gulp.parallel(buildSass, buildTs));
exports.buildTs = buildTs;
exports.buildSass = buildSass;
exports.clean = clean;
